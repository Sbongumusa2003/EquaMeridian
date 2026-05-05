// REPLACE EquaMeridian.Core/Interfaces/IEmailService.cs
public interface IEmailService
{
    Task SendLockoutEmailAsync(string email, string name);
    Task SendPasswordResetEmailAsync(string email, string name, string resetUrl);
    Task SendPasswordChangedNotificationAsync(string email, string name);
    Task SendAccountStatusChangedAsync(string email, string name, string newStatus);
    Task SendListingStatusChangedAsync(string email, string name,
                                       int listingId, string newStatus, string? reason);
    Task SendNewListingPendingReviewAsync(string adminEmail, int listingId, string supplierName);

    /// <summary>UC 5.1 Alt-Step 6b / Alt-Step 9a: Notify admin of duplicate listing.</summary>
    Task SendDuplicateListingFlaggedAsync(string adminEmail, int listingId,
                                          string listingTitle, string supplierName);

    /// <summary>UC 5.3 Alt-Step 5b: Notify admin that a ≥20% price change requires re-approval.</summary>
    Task SendListingPriceReApprovalRequiredAsync(string adminEmail, int listingId,
                                                  string listingTitle, string supplierName,
                                                  decimal oldPrice, decimal newPrice);
}