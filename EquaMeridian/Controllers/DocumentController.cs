using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/users/me/documents")]
[Authorize]
public class DocumentController : ControllerBase
{
    private readonly IDocumentRepository _repo;
    private readonly IAuditService _audit;
    private readonly AppDbContext _db;

    public DocumentController(IDocumentRepository repo, IAuditService audit, AppDbContext db)
    {
        _repo = repo;
        _audit = audit;
        _db = db;
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
        var docs = await _repo.GetByUserAsync(UserId);
        return Ok(docs);
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] int docTypeId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please add a document." });

        try
        {
            var docId = await _repo.UploadAsync(UserId, docTypeId, file);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _audit.LogAsync(UserId, "DOCUMENT_UPLOADED",
                $"Document '{file.FileName}' uploaded", null, null, null, ip);

            return Ok(new { message = "Document successfully added.", docId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{docId}")]
    public async Task<IActionResult> Replace(int docId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Please add a document." });

        var (success, message) = await _repo.ReplaceAsync(UserId, docId, file);

        if (!success)
        {
            if (message == "Not found.") return NotFound();
            return BadRequest(new { message });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(UserId, "DOCUMENT_REPLACED",
            $"Document {docId} replaced with '{file.FileName}'", null, null, null, ip);

        return Ok(new { message, docId });
    }
}