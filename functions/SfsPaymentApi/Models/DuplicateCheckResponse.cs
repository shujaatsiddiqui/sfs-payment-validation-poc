namespace SfsPaymentValidation.Models;

/// <summary>
/// Response model for duplicate payment check
/// </summary>
public class DuplicateCheckResponse
{
    /// <summary>
    /// Indicates whether the payment is a duplicate
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// Descriptive message about the duplicate check result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The composite key used for duplicate detection (paymentId:amount)
    /// </summary>
    public string CompositeKey { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }
}
