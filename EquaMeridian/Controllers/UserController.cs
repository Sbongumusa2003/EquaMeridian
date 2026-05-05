// REPLACE EquaMeridian/Controllers/UserController.cs
using EquaMeridian.DTOs.User;
using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly AppDbContext _db;

    public UsersController(IUserRepository repo, IAuditService audit,
                           IEmailService email, AppDbContext db)
    { _repo = repo; _audit = audit; _email = email; _db = db; }

    // ─── UC 1.2: View Users ───────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var (users, total) = await _repo.GetAllAsync(search, role, status, page, pageSize);

        await _audit.LogAsync(adminId, "ADMIN_PANEL_ACCESS",
            "Admin viewed user list", adminId, null, null, ip);

        return Ok(new { users, totalCount = total, page, pageSize });
    }

    // ─── UC 1.3: Get Audit Log for a user ────────────────────────────────────
    [HttpGet("{userId}/audit-log")]
    public async Task<IActionResult> GetAuditLog(int userId)
    {
        var logs = await _db.AuditLogs
            .Where(a => a.UserID == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .Select(a => new {
                a.AuditID,
                a.TransactionType,
                a.Description,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    // ─── UC 1.3: Update Account Status ───────────────────────────────────────
    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> UpdateStatus(
        int userId, [FromBody] UpdateAccountStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var user = await _repo.GetByIdAsync(userId);
        if (user == null) return NotFound();

        var previousStatus = user.AccountStatus;
        await _repo.UpdateStatusAsync(userId, dto.NewStatus);

        await _audit.LogAsync(userId, "ACCOUNT_STATUS_UPDATED",
            $"Status changed from {previousStatus} to {dto.NewStatus}",
            adminId, null, previousStatus, ip, dto.NewStatus);

        await _email.SendAccountStatusChangedAsync(user.Email, user.FullName, dto.NewStatus);

        return Ok(new { userId, newStatus = dto.NewStatus });
    }
}