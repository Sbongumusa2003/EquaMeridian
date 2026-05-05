using EquaMeridian.DTOs.Auth;
using EquaMeridian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IConfiguration config,
                       IEmailService email, IAuditService audit)
    { _db = db; _config = config; _email = email; _audit = audit; }

    public async Task<LoginResponse?> LoginAsync(LoginRequest dto, string ip)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return null;

        if (user.AccountStatus != "Active")
            throw new InvalidOperationException(user.AccountStatus);

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            user.FailedAttemptCount++;
            if (user.FailedAttemptCount >= 5)
            {
                user.AccountStatus = "Locked";
                user.LockoutExpiry = DateTime.UtcNow.AddMinutes(30);
                await _email.SendLockoutEmailAsync(user.Email, user.FullName);
                await _audit.LogAsync(user.UserID, "LOGIN_LOCKOUT",
                    "Account locked after 5 failed attempts", null, null, null, ip);
            }
            await _db.SaveChangesAsync();
            return null;
        }

        user.FailedAttemptCount = 0;
        user.LastLoginDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var expiry = dto.KeepMeSignedIn
            ? DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:LongExpiryDays"))
            : DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpiryMinutes"));

        var token = GenerateJwt(user, expiry);
        await _audit.LogAsync(user.UserID, "LOGIN", "Successful login", null, null, null, ip);

        return new LoginResponse
        {
            Token = token,
            Role = user.Role,   // returned as-is so Angular AuthService can lowercase it
            UserID = user.UserID,
            FullName = user.FullName,
            Expiry = expiry
        };
    }

    public async Task LogoutAsync(int userId, string token)
    {
        await _audit.LogAsync(userId, "LOGOUT", "User logged out", null, null, null, null);
    }

    public async Task ForgotPasswordAsync(string email, string ip)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.AccountStatus == "Active");
        if (user == null) return;

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var recentCount = await _db.PasswordReset
            .CountAsync(p => p.UserID == user.UserID && p.CreatedAt >= hourAgo);
        if (recentCount >= 3) return;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashToken(rawToken);

        _db.PasswordReset.Add(new PasswordReset
        {
            UserID = user.UserID,
            TokenHash = tokenHash,
            ExpiryTimestamp = DateTime.UtcNow.AddHours(24),
            IsUsed = false
        });
        await _db.SaveChangesAsync();

        var resetUrl = $"http://localhost:4200/auth/reset-password?token={Uri.EscapeDataString(rawToken)}";
        await _email.SendPasswordResetEmailAsync(user.Email, user.FullName, resetUrl);
        await _audit.LogAsync(user.UserID, "PASSWORD_RESET_REQUEST",
            "Reset email dispatched", null, null, null, ip);
    }

    public async Task<bool> ResetPasswordAsync(UpdatePasswordRequest dto, string ip)
    {
        var tokenHash = HashToken(dto.Token);
        var record = await _db.PasswordReset
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash
                && !p.IsUsed && p.ExpiryTimestamp > DateTime.UtcNow);

        if (record == null) return false;

        var user = await _db.Users.FindAsync(record.UserID);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        record.IsUsed = true;
        await _db.SaveChangesAsync();

        await _email.SendPasswordChangedNotificationAsync(user.Email, user.FullName);
        await _audit.LogAsync(user.UserID, "PASSWORD_RESET_COMPLETE",
            "Password reset via token", null, null, null, ip);
        return true;
    }

    private string GenerateJwt(User user, DateTime expiry)
    {
        // IMPORTANT: The role claim must match what the authorization policies expect.
        // Policy "AdminOnly"    => RequireRole("admin")    => store Role as "admin"
        // Policy "SupplierOnly" => RequireRole("Supplier") => store Role as "Supplier"
        // We store user.Role verbatim; callers must seed roles with the correct casing.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim(ClaimTypes.Name,           user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string raw)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}