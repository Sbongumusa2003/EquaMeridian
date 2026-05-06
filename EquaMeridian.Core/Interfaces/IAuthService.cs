using EquaMeridian.DTOs.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest dto, string ipAddress);
    Task LogoutAsync(int userId, string token);
    Task ForgotPasswordAsync(string email, string ipAddress);
    Task<bool> ResetPasswordAsync(UpdatePasswordRequest dto, string ipAddress);
    Task<(bool Success, string Message)> RegisterAsync(RegisterRequest dto, string ipAddress);
}