namespace SfsPaymentValidation.Models;

/// <summary>
/// Response from LM servicing system posting
/// </summary>
public class LMServicingResponse
{
    /// <summary>
    /// Whether the post was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// LM servicing transaction ID
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the post was made
    /// </summary>
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
}
