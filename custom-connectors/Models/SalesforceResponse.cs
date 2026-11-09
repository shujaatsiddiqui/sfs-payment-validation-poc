namespace SfsPaymentValidation.Models;

/// <summary>
/// Response from Salesforce unposted queue posting
/// </summary>
public class SalesforceResponse
{
    /// <summary>
    /// Whether the post was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Salesforce unposted queue record ID
    /// </summary>
    public string QueueId { get; set; } = string.Empty;

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the post was made
    /// </summary>
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}
