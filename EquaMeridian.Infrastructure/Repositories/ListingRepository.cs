using EquaMeridian.DTOs.Listings;
using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class ListingRepository : IListingRepository
{
    private readonly AppDbContext _db;
    public ListingRepository(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<ListingDto>, int)> GetAllAsync(
        string? search, int? category, string? status, int page, int pageSize)
    {
        var q = _db.Listings
            .Include(l => l.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(l => l.ListingTitle.Contains(search) ||
                             l.Supplier.FullName.Contains(search));

        if (category.HasValue) q = q.Where(l => l.CategoryID == category.Value);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(l => l.AvailabilityStatus == status);

        var total = await q.CountAsync();
        var listings = await q
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var ids = listings.Select(l => l.ListingID).ToList();
        var imageMap = await BuildImageMapAsync(ids);

        return (listings.Select(l => MapToDto(l, imageMap)), total);
    }

    public async Task<ListingDto?> GetByIdAsync(int id)
    {
        var listing = await _db.Listings
            .Include(l => l.Supplier)
            .FirstOrDefaultAsync(l => l.ListingID == id);

        if (listing == null) return null;

        var imageMap = await BuildImageMapAsync(new[] { id });
        return MapToDto(listing, imageMap);
    }

    public async Task<IEnumerable<ListingDto>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.ToList();
        var listings = await _db.Listings
            .Include(l => l.Supplier)
            .Where(l => idList.Contains(l.ListingID))
            .ToListAsync();

        var imageMap = await BuildImageMapAsync(idList);
        return listings.Select(l => MapToDto(l, imageMap));
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        var l = await _db.Listings.FindAsync(id) ?? throw new KeyNotFoundException();
        l.AvailabilityStatus = status;
        await _db.SaveChangesAsync();
    }

    public async Task<(IEnumerable<ListingDto>, int)> GetBySupplierAsync(
        int supplierId, string? status, int page, int pageSize)
    {
        var q = _db.Listings
            .Include(l => l.Supplier)
            .Where(l => l.SupplierID == supplierId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(l => l.AvailabilityStatus == status);

        var total = await q.CountAsync();
        var listings = await q
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var ids = listings.Select(l => l.ListingID).ToList();
        var imageMap = await BuildImageMapAsync(ids);

        return (listings.Select(l => MapToDto(l, imageMap)), total);
    }

    public async Task<int> CreateAsync(CreateListingDto dto, int supplierId)
    {
        var isDuplicate = await _db.Listings.AnyAsync(l =>
            l.SupplierID == supplierId &&
            l.ListingTitle.ToLower() == dto.ListingTitle.ToLower());

        var listing = new Listing
        {
            ListingTitle = dto.ListingTitle,
            CategoryID = dto.CategoryID,
            Description = dto.Description,
            MakeBrand = dto.MakeBrand,
            Model = dto.Model,
            Year = dto.Year,
            OperatingWeight = dto.OperatingWeight,
            EnginePower = dto.EnginePower,
            Location = dto.Location,
            DailyRateZAR = dto.DailyRateZAR,
            WeeklyRateZAR = dto.WeeklyRateZAR,
            AvailabilityStatus = "Pending",
            SupplierID = supplierId,
            DuplicateFlag = isDuplicate,
            CreatedDate = DateTime.UtcNow
        };
        _db.Listings.Add(listing);
        await _db.SaveChangesAsync();
        return listing.ListingID;
    }

    public async Task UpdateAsync(int id, UpdateListingDto dto)
    {
        var l = await _db.Listings.FindAsync(id) ?? throw new KeyNotFoundException();
        var priceChange = l.DailyRateZAR == 0
            ? 1m
            : Math.Abs((dto.DailyRateZAR - l.DailyRateZAR) / l.DailyRateZAR);

        l.ListingTitle = dto.ListingTitle;
        l.CategoryID = dto.CategoryID;
        l.Description = dto.Description;
        l.MakeBrand = dto.MakeBrand;
        l.Model = dto.Model;
        l.Year = dto.Year;
        l.OperatingWeight = dto.OperatingWeight;
        l.EnginePower = dto.EnginePower;
        l.Location = dto.Location;
        l.DailyRateZAR = dto.DailyRateZAR;
        l.WeeklyRateZAR = dto.WeeklyRateZAR;

        if (priceChange >= 0.20m) l.AvailabilityStatus = "Pending";

        await _db.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var l = await _db.Listings.FindAsync(id) ?? throw new KeyNotFoundException();
        l.AvailabilityStatus = "Inactive";
        l.DeactivatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
    private async Task<Dictionary<int, List<string>>> BuildImageMapAsync(IEnumerable<int> listingIds)
    {
        var ids = listingIds.ToList();

        var rows = await _db.ListingImages
            .Where(i => ids.Contains(i.ListingID))
            .OrderBy(i => i.ListingID)
            .ThenBy(i => i.DisplayOrder)
            .Select(i => new { i.ListingID, i.FilePath })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ListingID)
            .ToDictionary(g => g.Key, g => g.Select(r => r.FilePath).ToList());
    }

    private static ListingDto MapToDto(Listing l, Dictionary<int, List<string>> imageMap) => new()
    {
        ListingID = l.ListingID,
        ListingTitle = l.ListingTitle,
        CategoryID = l.CategoryID,
        AvailabilityStatus = l.AvailabilityStatus,
        Description = l.Description,
        MakeBrand = l.MakeBrand,
        Model = l.Model,
        Year = l.Year,
        OperatingWeight = l.OperatingWeight,
        EnginePower = l.EnginePower,
        Location = l.Location,
        DailyRateZAR = l.DailyRateZAR,
        WeeklyRateZAR = l.WeeklyRateZAR,
        CreatedDate = l.CreatedDate,
        DuplicateFlag = l.DuplicateFlag,
        SupplierID = l.SupplierID,
        SupplierName = l.Supplier?.FullName ?? string.Empty,
        ImageUrls = imageMap.TryGetValue(l.ListingID, out var urls) ? urls : new List<string>()
    };
}