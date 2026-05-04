public class User
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "Pending";
    public int FailedAttemptCount { get; set; } = 0;
    public DateTime? LockoutExpiry { get; set; }
    public string? CompanyName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}