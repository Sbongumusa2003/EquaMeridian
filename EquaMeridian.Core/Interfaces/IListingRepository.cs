public interface IListingRepository
{
    // Admin
    Task<(IEnumerable<ListingDto> Listings, int TotalCount)> GetAllAsync(
        string? search, int? category, string? status, int page, int pageSize);
    Task<ListingDto?> GetByIdAsync(int listingId);
    Task UpdateStatusAsync(int listingId, string newStatus);

    // Supplier
    Task<(IEnumerable<ListingDto> Listings, int TotalCount)> GetBySupplierAsync(
        int supplierId, string? status, int page, int pageSize);
    Task<int> CreateAsync(CreateListingDto dto, int supplierId);
    Task UpdateAsync(int listingId, UpdateListingDto dto);
    Task DeactivateAsync(int listingId);
}
