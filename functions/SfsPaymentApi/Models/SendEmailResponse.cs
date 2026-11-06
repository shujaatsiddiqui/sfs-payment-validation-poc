namespace SfsPaymentValidation.Models;

/// <summary>
/// Response model for email sending operation
/// </summary>
public class SendEmailResponse
{
    /// <summary>
    /// Indicates if email was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of recipients email was sent to
    /// </summary>
    public int RecipientCount { get; set; }

    /// <summary>
    /// Timestamp when email was sent
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Status message or error details
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Twilio message SID (for tracking)
    /// </summary>
    public string? MessageId { get; set; }
}
