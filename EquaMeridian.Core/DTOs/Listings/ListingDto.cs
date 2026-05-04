namespace EquaMeridian.DTOs.Listings
{
    public class ListingDto
    {
        public int ListingID { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string AvailabilityStatus { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? MakeBrand { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? OperatingWeight { get; set; }
        public string? EnginePower { get; set; }
        public string? Location { get; set; }
        public decimal DailyRateZAR { get; set; }
        public decimal? WeeklyRateZAR { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool DuplicateFlag { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }
}