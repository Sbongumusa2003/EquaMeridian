public class EmailService : IEmailService
{
    private readonly IAuditService _audit;
    public EmailService(IAuditService audit) => _audit = audit;

    private async Task SendWithRetryAsync(Func<Task> send,
        int userId, string auditType)
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
            }
        }
    }

    public Task SendLockoutEmailAsync(string email, string name)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Lockout notice to {email}");
            return Task.CompletedTask;
        }, 0, "SendLockoutEmail");

    public Task SendPasswordResetEmailAsync(string email, string name, string url)
        => SendWithRetryAsync(() => {
            Console.WriteLine($"[EMAIL] Reset link to {email}: {url}");
            return Task.CompletedTask;
        }, 0, "SendPasswordResetEmail");
    public Task SendPasswordChangedNotificationAsync(string e, string n)
        => Task.CompletedTask;
    public Task SendAccountStatusChangedAsync(string e, string n, string s)
        => Task.CompletedTask;
    public Task SendListingStatusChangedAsync(string e, string n, int id, string s, string? r)
        => Task.CompletedTask;
    public Task SendNewListingPendingReviewAsync(string e, int id, string s)
        => Task.CompletedTask;
}
