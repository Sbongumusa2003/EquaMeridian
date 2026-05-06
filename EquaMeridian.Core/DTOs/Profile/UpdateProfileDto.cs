using System.ComponentModel.DataAnnotations;

namespace EquaMeridian.DTOs.User
{
    public class UpdateProfileDto
    {
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        [MaxLength(100)]
        public string? RegistrationNumber { get; set; }
    }
}