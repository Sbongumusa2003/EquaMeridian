using Microsoft.AspNetCore.Http;

public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetByUserAsync(int userId);
    Task<int> UploadAsync(int userId, int docTypeId, IFormFile file);
    Task<(bool Success, string Message)> ReplaceAsync(int userId, int docId, IFormFile file);
}