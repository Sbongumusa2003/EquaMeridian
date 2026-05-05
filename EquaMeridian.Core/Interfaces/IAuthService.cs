// REPLACE EquaMeridian.Core/Interfaces/IAuthService.cs
using EquaMeridian.DTOs.Auth;
using Microsoft.IdentityModel.Tokens;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest dto, string ipAddress);
    Task LogoutAsync(int userId, string token);
    Task ForgotPasswordAsync(string email, string ipAddress);

    /// <summary>UC 7.10 Step 1: Pre-validate token without consuming it.</summary>
    Task<TokenValidationResult> ValidateResetTokenAsync(string rawToken);

    Task<bool> ResetPasswordAsync(UpdatePasswordRequest dto, string ipAddress);
}