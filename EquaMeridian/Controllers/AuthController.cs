using EquaMeridian.DTOs.Auth;
using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppDbContext _db;

    public AuthController(IAuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            var result = await _auth.LoginAsync(dto, ip);
            if (result == null)
                return Unauthorized(new { message = "Incorrect email or password." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = $"Account is {ex.Message}." });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await _auth.LogoutAsync(userId, token);
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _auth.ForgotPasswordAsync(dto.Email, ip);
        return Ok(new { message = "If that email is registered, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] UpdatePasswordRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var success = await _auth.ResetPasswordAsync(dto, ip);
        if (!success)
            return BadRequest(new { message = "Token is invalid, expired, or already used." });
        return Ok(new { message = "Password reset successfully." });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            CompanyName = dto.CompanyName,
            AccountStatus = "Pending",
            CreatedDate = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Account created. Awaiting admin approval." });
    }
}