namespace SfsPaymentValidation.Models;

/// <summary>
/// Request model for sending email via Twilio Email API
/// </summary>
public class SendEmailRequest
{
    /// <summary>
    /// Recipient email address(es)
    /// </summary>
    public List<string> To { get; set; } = new();

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body content
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Optional plain text body (fallback for non-HTML clients)
    /// </summary>
    public string? TextBody { get; set; }
}
