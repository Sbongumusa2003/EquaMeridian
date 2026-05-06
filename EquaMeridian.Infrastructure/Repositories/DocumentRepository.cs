using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class DocumentRepository : IDocumentRepository
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private readonly AppDbContext _db;

    public DocumentRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Document>> GetByUserAsync(int userId)
        => await _db.Documents
            .Where(d => d.UserID == userId)
            .ToListAsync();

    public async Task<int> UploadAsync(int userId, int docTypeId, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Unsupported file format. Please upload PDF, JPG, or PNG.");

        var uploadPath = Path.Combine("Uploads", "Documents", userId.ToString());
        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadPath, fileName);

        using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        var doc = new Document
        {
            DocTypeID = docTypeId,
            DocName = file.FileName,
            FilePath = fullPath,
            VerificationStatus = "Pending",
            UserID = userId
        };
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        return doc.DocID;
    }

    public async Task<(bool Success, string Message)> ReplaceAsync(
        int userId, int docId, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "Unsupported file format.");

        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.DocID == docId && d.UserID == userId);

        if (doc == null) return (false, "Not found.");

        if (File.Exists(doc.FilePath))
            File.Delete(doc.FilePath);

        var uploadPath = Path.GetDirectoryName(doc.FilePath)!;
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadPath, fileName);

        using (var stream = File.Create(fullPath))
            await file.CopyToAsync(stream);

        doc.DocName = file.FileName;
        doc.FilePath = fullPath;
        doc.VerificationStatus = "Pending";
        doc.UploadedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, "Document successfully replaced.");
    }
}