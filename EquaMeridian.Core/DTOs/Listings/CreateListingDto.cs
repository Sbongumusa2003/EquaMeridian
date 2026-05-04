using System.ComponentModel.DataAnnotations;

namespace EquaMeridian.DTOs.Listings
{
    public class CreateListingDto
    {
        [Required]
        [MaxLength(200)]
        public string ListingTitle { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public string? MakeBrand { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? OperatingWeight { get; set; }
        public string? EnginePower { get; set; }
        public string? Location { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Daily rate must be greater than zero.")]
        public decimal DailyRateZAR { get; set; }

        public decimal? WeeklyRateZAR { get; set; }
    }
}