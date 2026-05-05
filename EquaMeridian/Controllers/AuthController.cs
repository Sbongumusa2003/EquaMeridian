// REPLACE EquaMeridian/Controllers/AuthController.cs
using EquaMeridian.DTOs.Auth;
using EquaMeridian.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

    // ─── UC 6.7: Login ────────────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            var result = await _auth.LoginAsync(dto, ip);
            if (result == null)
                return Unauthorized(new { message = "Incorrect email or password. Please try again." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // UC 6.7 Step 4c: Return status-specific message so Angular can display it
            var status = ex.Message;
            var message = status switch
            {
                "Locked" => "Your account has been temporarily locked due to multiple failed login attempts. Please try again in 30 minutes or contact support.",
                "Pending" => "Your account is pending approval by an administrator. You will be notified by email once your account is activated.",
                "Disabled" => "Your account has been disabled. Please contact support.",
                "Suspended" => "Your account has been suspended. Please contact support.",
                _ => $"Account access denied: {status}."
            };
            return StatusCode(403, new { message });
        }
    }

    // ─── UC 6.8: Logout ───────────────────────────────────────────────────────
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        await _auth.LogoutAsync(userId, token);
        return Ok(new { message = "Logged out successfully." });
    }

    // ─── UC 6.9: Forgot Password ──────────────────────────────────────────────
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        try
        {
            await _auth.ForgotPasswordAsync(dto.Email, ip);
            // Always return 200 to prevent email enumeration (UC 6.9 Step 3a)
            return Ok(new { message = "If that email is registered, a reset link has been sent." });
        }
        catch (Exception)
        {
            // UC 6.9 Step 5a: Surface email dispatch failure
            return StatusCode(500, new { message = "We were unable to send the reset email. Please try again later." });
        }
    }

    // ─── UC 7.10: Validate Reset Token (pre-load check) ──────────────────────
    /// <summary>
    /// UC 7.10 Step 1: Called by the Angular component on ngOnInit to validate
    /// the token BEFORE rendering the update-password form.
    /// Returns 200 if valid, 400 with reason if expired or already used.
    /// </summary>
    [HttpGet("validate-reset-token")]
    public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { message = "Token is required." });

        var result = await _auth.ValidateResetTokenAsync(token);
        return result switch
        {
            TokenValidationResult.Valid => Ok(new { valid = true }),
            TokenValidationResult.Expired => BadRequest(new { message = "This password reset link has expired. Reset links are valid for 24 hours." }),
            TokenValidationResult.AlreadyUsed => BadRequest(new { message = "This password reset link has already been used." }),
            _ => BadRequest(new { message = "This password reset link is invalid." })
        };
    }

    // ─── UC 7.10: Reset Password ──────────────────────────────────────────────
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

    // ─── Register ─────────────────────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null)
            return Conflict(new { message = "An account with this email already exists." });

        var normalisedRole = dto.Role.ToLower() switch
        {
            "admin" => "admin",
            "supplier" => "Supplier",
            "contractor" => "contractor",
            _ => dto.Role
        };

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = normalisedRole,
            CompanyName = dto.CompanyName,
            AccountStatus = "Pending",
            CreatedDate = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Account created. Awaiting admin approval." });
    }
}