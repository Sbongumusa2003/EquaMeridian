using EquaMeridian.DTOs.Listings;

public interface IListingRepository
{
    Task<(IEnumerable<ListingDto> Listings, int TotalCount)> GetAllAsync(
        string? search, int? category, string? status, int page, int pageSize);
    Task<ListingDto?> GetByIdAsync(int listingId);
    Task<IEnumerable<ListingDto>> GetByIdsAsync(IEnumerable<int> ids);
    Task UpdateStatusAsync(int listingId, string newStatus);
    Task<(IEnumerable<ListingDto> Listings, int TotalCount)> GetBySupplierAsync(
        int supplierId, string? status, int page, int pageSize);
    Task<int> CreateAsync(CreateListingDto dto, int supplierId);
    Task UpdateAsync(int listingId, UpdateListingDto dto);
    Task DeactivateAsync(int listingId);
}