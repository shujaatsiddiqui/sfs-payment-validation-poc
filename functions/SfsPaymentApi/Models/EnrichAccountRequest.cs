namespace SfsPaymentValidation.Models;

/// <summary>
/// Request model for account enrichment
/// </summary>
public class EnrichAccountRequest
{
    /// <summary>
    /// The payment identifier used to lookup associated account
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;
}
