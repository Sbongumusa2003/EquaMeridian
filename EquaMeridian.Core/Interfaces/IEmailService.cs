public interface IEmailService
{
    Task SendLockoutEmailAsync(string email, string name);
    Task SendPasswordResetEmailAsync(string email, string name, string resetUrl);
    Task SendPasswordChangedNotificationAsync(string email, string name);
    Task SendAccountStatusChangedAsync(string email, string name, string newStatus);
    Task SendListingStatusChangedAsync(string email, string name,
                                       int listingId, string newStatus, string? reason);
    Task SendNewListingPendingReviewAsync(string adminEmail, int listingId, string supplierName);
}
