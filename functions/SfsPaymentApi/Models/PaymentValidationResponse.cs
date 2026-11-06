namespace SfsPaymentValidation.Models;

/// <summary>
/// Response model for comprehensive payment validation
/// </summary>
public class PaymentValidationResponse
{
    /// <summary>
    /// Indicates whether the payment passed all validations
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Payment type classification: Standard, Payoff, or Overpayment
    /// </summary>
    public string PaymentType { get; set; } = string.Empty;

    /// <summary>
    /// List of validation errors (empty if payment is valid)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of business rules that were checked
    /// </summary>
    public List<string> RulesChecked { get; set; } = new();

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; }
}
