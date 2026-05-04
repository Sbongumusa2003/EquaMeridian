public interface IUserRepository
{
    Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAllAsync(
        string? search, string? role, string? status, int page, int pageSize);
    Task<User?> GetByIdAsync(int userId);
    Task UpdateStatusAsync(int userId, string newStatus);
}
