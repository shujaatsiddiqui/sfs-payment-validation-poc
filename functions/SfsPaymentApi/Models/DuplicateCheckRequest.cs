namespace SfsPaymentValidation.Models;

/// <summary>
/// Request model for duplicate payment check
/// </summary>
public class DuplicateCheckRequest
{
    /// <summary>
    /// The payment identifier to check
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// The payment amount (used for composite key)
    /// </summary>
    public decimal Amount { get; set; }
}
