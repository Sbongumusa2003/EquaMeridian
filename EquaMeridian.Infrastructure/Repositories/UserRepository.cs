using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<(IEnumerable<UserDto> Users, int TotalCount)> GetAllAsync(
        string? search, string? role, string? status, int page, int pageSize)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FullName.Contains(search) ||
                u.Email.Contains(search) ||
                (u.CompanyName != null && u.CompanyName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(u => u.AccountStatus == status);

        var total = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                UserID = u.UserID,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                AccountStatus = u.AccountStatus,
                CompanyName = u.CompanyName,
                CreatedDate = u.CreatedDate,
                LastLoginDate = u.LastLoginDate
            }).ToListAsync();

        return (users, total);
    }

    public Task<User?> GetByIdAsync(int userId)
        => _db.Users.FirstOrDefaultAsync(u => u.UserID == userId);

    public async Task UpdateStatusAsync(int userId, string newStatus)
    {
        var user = await _db.Users.FindAsync(userId)
                   ?? throw new KeyNotFoundException();
        user.AccountStatus = newStatus;
        await _db.SaveChangesAsync();
    }
}
