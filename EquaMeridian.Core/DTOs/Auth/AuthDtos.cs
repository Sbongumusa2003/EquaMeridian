using System.ComponentModel.DataAnnotations;

namespace EquaMeridian.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool KeepMeSignedIn { get; set; } = false;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Supplier";

        public string? CompanyName { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class UpdatePasswordRequest
    {
        [Required] public string Token { get; set; } = string.Empty;
        [Required][MinLength(8)] public string NewPassword { get; set; } = string.Empty;
        [Required][Compare("NewPassword")] public string ConfirmPassword { get; set; } = string.Empty;
    }
}