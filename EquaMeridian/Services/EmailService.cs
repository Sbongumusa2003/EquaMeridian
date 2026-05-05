// REPLACE EquaMeridian/Services/EmailService.cs
public class EmailService : IEmailService
{
    private readonly IAuditService _audit;
    public EmailService(IAuditService audit) => _audit = audit;

    private async Task SendWithRetryAsync(Func<Task> send, int userId, string auditType)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try { await send(); return; }
            catch when (attempt < 3) { await Task.Delay(500 * attempt); }
            catch
            {
                await _audit.LogAsync(userId, "NOTIFICATION_DISPATCH_FAILED",
                    $"{auditType} failed after 3 attempts",
                    null, null, null, null);
                // Re-throw so the caller (e.g. ForgotPasswordAsync) can surface it
                throw;
            }
        }
    }

    public Task SendLockoutEmailAsync(string email, string name)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Lockout notice → {email} ({name})");
            return Task.CompletedTask;
        }, 0, "SendLockoutEmail");

    public Task SendPasswordResetEmailAsync(string email, string name, string url)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Password reset link → {email}: {url}");
            return Task.CompletedTask;
        }, 0, "SendPasswordResetEmail");

    public Task SendPasswordChangedNotificationAsync(string email, string name)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Password changed notification → {email} ({name})");
            return Task.CompletedTask;
        }, 0, "SendPasswordChangedNotification");

    public Task SendAccountStatusChangedAsync(string email, string name, string status)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Account status changed → {email} ({name}): {status}");
            return Task.CompletedTask;
        }, 0, "SendAccountStatusChanged");

    public Task SendListingStatusChangedAsync(string email, string name,
        int listingId, string status, string? reason)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Listing #{listingId} status → {status} for {name} ({email}). Reason: {reason}");
            return Task.CompletedTask;
        }, 0, "SendListingStatusChanged");

    public Task SendNewListingPendingReviewAsync(string adminEmail, int listingId, string supplierName)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] New listing #{listingId} pending review from {supplierName} → {adminEmail}");
            return Task.CompletedTask;
        }, 0, "SendNewListingPendingReview");

    /// <summary>UC 5.1 Alt-Step 6b / 9a: Duplicate listing flagged — notify admin.</summary>
    public Task SendDuplicateListingFlaggedAsync(string adminEmail, int listingId,
        string listingTitle, string supplierName)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Duplicate listing flagged: #{listingId} '{listingTitle}' by {supplierName} → {adminEmail}");
            return Task.CompletedTask;
        }, 0, "SendDuplicateListingFlagged");

    /// <summary>UC 5.3 Alt-Step 5b: Price change ≥20% — notify admin of re-approval requirement.</summary>
    public Task SendListingPriceReApprovalRequiredAsync(string adminEmail, int listingId,
        string listingTitle, string supplierName, decimal oldPrice, decimal newPrice)
        => SendWithRetryAsync(() => {
            var changePct = oldPrice > 0
                ? Math.Abs((newPrice - oldPrice) / oldPrice) * 100
                : 0;
            Console.WriteLine(
                $"[EMAIL] Price re-approval required: #{listingId} '{listingTitle}' by {supplierName}. " +
                $"Price: R{oldPrice:N2} → R{newPrice:N2} ({changePct:N1}% change) → {adminEmail}");
            return Task.CompletedTask;
        }, 0, "SendListingPriceReApprovalRequired");
}