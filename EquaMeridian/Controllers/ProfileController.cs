using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EquaMeridian.DTOs.User;

[ApiController]
[Route("api/users/me")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ProfileController(IUserRepository repo, AppDbContext db, IAuditService audit)
    {
        _repo = repo;
        _db = db;
        _audit = audit;
    }

    // GET /api/users/me
    [HttpGet]
    public async Task<IActionResult> GetMyAccount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _repo.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "Account details could not be found. Please contact support." });

        return Ok(new
        {
            user.UserID,
            user.FullName,
            user.Email,
            user.Role,
            user.AccountStatus,
            user.CompanyName,
            user.CreatedDate,
            user.LastLoginDate
        });
    }

    // PUT /api/users/me/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (user.Email != dto.Email)
        {
            var taken = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.UserID != userId);
            if (taken) return Conflict(new { message = "Email already in use." });
        }

        var previousValues = $"Name:{user.FullName}, Email:{user.Email}";
        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.CompanyName = dto.CompanyName;
        await _db.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(userId, "PROFILE_UPDATED",
            "User updated their profile", null, null,
            previousValues, ip, $"Name:{dto.FullName}, Email:{dto.Email}");

        return Ok(new { message = $"User {user.FullName} successfully updated." });
    }

    // PATCH /api/users/me/deactivate
    [HttpPatch("deactivate")]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var hasActiveListings = await _db.Listings.AnyAsync(
            l => l.SupplierID == userId && l.AvailabilityStatus == "Active");
        if (hasActiveListings)
            return BadRequest(new { message = "Cannot deactivate account: you have active listings. Please deactivate them first." });

        user.AccountStatus = "Inactive";
        await _db.SaveChangesAsync();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _audit.LogAsync(userId, "ACCOUNT_DEACTIVATED",
            "User deactivated their own account", null, null, "Active", ip, "Inactive");

        return Ok(new { message = $"User {user.FullName} account successfully deactivated." });
    }
}