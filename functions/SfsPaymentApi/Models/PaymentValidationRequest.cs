namespace SfsPaymentValidation.Models;

/// <summary>
/// Request model for comprehensive payment validation (with enriched account data)
/// </summary>
public class PaymentValidationRequest
{
    /// <summary>
    /// The payment identifier (for logging)
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount to validate
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Current account balance (from EnrichAccount)
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Account status code (from EnrichAccount)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Closed to posting flag - 0 = open, 1 = closed (from EnrichAccount)
    /// </summary>
    public int ClosedToPosting { get; set; }
}
