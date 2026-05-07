using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class ListingImageRepository : IListingImageRepository
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const int MaxImagesPerListing = 5;

    private readonly AppDbContext _db;

    public ListingImageRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<string>> GetUrlsByListingAsync(int listingId)
        => await _db.ListingImages
            .Where(i => i.ListingID == listingId)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.FilePath)
            .ToListAsync();

    public async Task<IEnumerable<string>> AddImagesAsync(int listingId, IEnumerable<IFormFile> files)
    {
        var existingCount = await _db.ListingImages.CountAsync(i => i.ListingID == listingId);
        var saved = new List<string>();
        var order = existingCount;

        var uploadPath = Path.Combine("Uploads", "Listings", listingId.ToString());
        Directory.CreateDirectory(uploadPath);

        foreach (var file in files)
        {
            if (existingCount + saved.Count >= MaxImagesPerListing)
                break;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                continue;

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadPath, fileName);
            var relativeUrl = $"/uploads/listings/{listingId}/{fileName}";

            using (var stream = File.Create(fullPath))
                await file.CopyToAsync(stream);

            _db.ListingImages.Add(new ListingImage
            {
                ListingID = listingId,
                FilePath = relativeUrl,
                DisplayOrder = order++,
                UploadedDate = DateTime.UtcNow
            });

            saved.Add(relativeUrl);
        }

        if (saved.Count > 0)
            await _db.SaveChangesAsync();

        return saved;
    }

    public async Task<bool> DeleteAsync(int listingId, int imageId)
    {
        var image = await _db.ListingImages
            .FirstOrDefaultAsync(i => i.ImageID == imageId && i.ListingID == listingId);

        if (image == null) return false;
        var wwwRoot = Directory.GetCurrentDirectory();
        var filePath = Path.Combine(wwwRoot, image.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
            File.Delete(filePath);

        _db.ListingImages.Remove(image);
        await _db.SaveChangesAsync();
        return true;
    }
}