using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/users/me/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public DocumentController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private int UserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var types = await _db.DocumentTypes.ToListAsync();
        return Ok(types);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDocuments()
    {
        var docs = await _db.Documents
            .Where(d => d.UserID == UserId)
            .ToListAsync();
        return Ok(docs);
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] int docTypeId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please add a document." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Unsupported file format. Please upload PDF, JPG, or PNG." });

        var uploadPath = Path.Combine("Uploads", "Documents", UserId.ToString());
        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadPath, fileName);

        using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        var doc = new Document
        {
            DocTypeID = docTypeId,
            DocName = file.FileName,
            FilePath = fullPath,
            VerificationStatus = "Pending",
            UserID = UserId
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(UserId, "DOCUMENT_UPLOADED",
            $"Document '{file.FileName}' uploaded", null, null, null, ip);

        return Ok(new { message = "Document successfully added.", doc.DocID });
    }

    [HttpPut("{docId}")]
    public async Task<IActionResult> Replace(int docId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please add a document." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = "Unsupported file format." });

        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.DocID == docId && d.UserID == UserId);
        if (doc == null) return NotFound();

        if (System.IO.File.Exists(doc.FilePath))
            System.IO.File.Delete(doc.FilePath);

        var uploadPath = Path.GetDirectoryName(doc.FilePath)!;
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadPath, fileName);

        using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        doc.DocName = file.FileName;
        doc.FilePath = fullPath;
        doc.VerificationStatus = "Pending";
        doc.UploadedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(UserId, "DOCUMENT_REPLACED",
            $"Document {docId} replaced with '{file.FileName}'", null, null, null, ip);

        return Ok(new { message = "Document successfully replaced.", docId });
    }
}