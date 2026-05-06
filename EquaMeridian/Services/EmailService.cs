using SendGrid;
using SendGrid.Helpers.Mail;

public class EmailService : IEmailService
{
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IAuditService audit, IConfiguration config)
    {
        _audit = audit;
        _config = config;
        _fromEmail = _config["Email:FromAddress"] ?? "noreply@equameridian.co.za";
        _fromName = _config["Email:FromName"] ?? "EquaMeridian";
    }

    public Task SendLockoutEmailAsync(string email, string name)
        => SendWithRetryAsync(
            () => SendAsync(
                to: email,
                toName: name,
                subject: "Your EquaMeridian account has been locked",
                body: $"Hi {name},<br><br>"
                    + "Your account has been locked after 5 failed login attempts.<br>"
                    + "It will automatically unlock after 30 minutes.<br><br>"
                    + "If this was not you, please contact support immediately."
            ),
            auditType: "SendLockoutEmail"
        );

    public Task SendPasswordResetEmailAsync(string email, string name, string resetUrl)
        => SendWithRetryAsync(
            () => SendAsync(
                to: email,
                toName: name,
                subject: "Reset your EquaMeridian password",
                body: $"Hi {name},<br><br>"
                    + "Click the link below to reset your password. "
                    + "This link expires in 24 hours.<br><br>"
                    + $"<a href=\"{resetUrl}\">Reset Password</a><br><br>"
                    + "If you did not request this, you can safely ignore this email."
            ),
            auditType: "SendPasswordResetEmail"
        );

    public Task SendPasswordChangedNotificationAsync(string email, string name)
        => SendWithRetryAsync(
            () => SendAsync(
                to: email,
                toName: name,
                subject: "Your EquaMeridian password was changed",
                body: $"Hi {name},<br><br>"
                    + "Your password was successfully changed.<br>"
                    + "If you did not make this change, please contact support immediately."
            ),
            auditType: "SendPasswordChangedNotification"
        );

    public Task SendAccountStatusChangedAsync(string email, string name, string newStatus)
        => SendWithRetryAsync(
            () => SendAsync(
                to: email,
                toName: name,
                subject: "Your EquaMeridian account status has changed",
                body: $"Hi {name},<br><br>"
                    + $"Your account status has been updated to: <strong>{newStatus}</strong>.<br><br>"
                    + "If you have any questions, please contact support."
            ),
            auditType: "SendAccountStatusChanged"
        );

    public Task SendListingStatusChangedAsync(string email, string name,
        int listingId, string newStatus, string? reason)
    {
        var reasonPart = !string.IsNullOrWhiteSpace(reason)
            ? $"<br><br>Reason: {reason}"
            : string.Empty;

        return SendWithRetryAsync(
            () => SendAsync(
                to: email,
                toName: name,
                subject: $"Your listing #{listingId} status has been updated",
                body: $"Hi {name},<br><br>"
                    + $"The status of your listing <strong>#{listingId}</strong> "
                    + $"has been changed to: <strong>{newStatus}</strong>."
                    + $"{reasonPart}<br><br>"
                    + "Log in to your supplier dashboard to view the listing."
            ),
            auditType: "SendListingStatusChanged"
        );
    }

    public Task SendNewListingPendingReviewAsync(string adminEmail, int listingId, string supplierName)
        => SendWithRetryAsync(
            () => SendAsync(
                to: adminEmail,
                toName: "Admin",
                subject: $"New listing #{listingId} pending review",
                body: $"A new listing (ID: <strong>{listingId}</strong>) was submitted by "
                    + $"<strong>{supplierName}</strong> and is awaiting your review.<br><br>"
                    + "Log in to the admin panel to approve or reject."
            ),
            auditType: "SendNewListingPendingReview"
        );

    private async Task SendAsync(string to, string toName, string subject, string body)
    {
        var apiKey = _config["Email:SendGridApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine($"[EMAIL] (no API key) To: {to} | Subject: {subject}");
            return;
        }

        var client = new SendGridClient(apiKey);
        var msg = MailHelper.CreateSingleEmail(
            from: new EmailAddress(_fromEmail, _fromName),
            to: new EmailAddress(to, toName),
            subject: subject,
            plainTextContent: System.Text.RegularExpressions.Regex.Replace(body, "<.*?>", ""),
            htmlContent: body
        );

        var response = await client.SendEmailAsync(msg);

        if ((int)response.StatusCode >= 400)
        {
            var responseBody = await response.Body.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"SendGrid returned {response.StatusCode}: {responseBody}");
        }
    }

    private async Task SendWithRetryAsync(Func<Task> send, string auditType)
    {
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await send();
                return;
            }
            catch when (attempt < 3)
            {
                await Task.Delay(500 * attempt);
            }
            catch (Exception ex)
            {
                await _audit.LogAsync(null, "NOTIFICATION_DISPATCH_FAILED",
                    $"{auditType} failed after 3 attempts: {ex.Message}",
                    null, null, null, null);
            }
        }
    }
}