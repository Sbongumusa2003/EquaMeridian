public class Listing
{
    public int ListingID { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public int CategoryID { get; set; }
    public string AvailabilityStatus { get; set; } = "Pending";
    public string Description { get; set; } = string.Empty;
    public string? MakeBrand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? OperatingWeight { get; set; }
    public string? EnginePower { get; set; }
    public string? Location { get; set; }
    public decimal DailyRateZAR { get; set; }
    public decimal? WeeklyRateZAR { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeactivatedDate { get; set; }
    public bool DuplicateFlag { get; set; } = false;
    public int SupplierID { get; set; }
    public User Supplier { get; set; } = null!;
}
