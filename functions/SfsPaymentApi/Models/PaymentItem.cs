namespace SfsPaymentValidation.Models;

/// <summary>
/// Payment item for processing
/// </summary>
public class PaymentItem
{
    /// <summary>
    /// Payment identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }
}
