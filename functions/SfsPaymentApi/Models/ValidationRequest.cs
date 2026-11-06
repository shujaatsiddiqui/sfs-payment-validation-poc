namespace SfsPaymentValidation.Models;

/// <summary>
/// Request model for payment validation
/// </summary>
public class ValidationRequest
{
    /// <summary>
    /// The payment identifier to validate
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Rule selection: "All Rules", "BR-005", "BR-008"
    /// </summary>
    public string RuleSelection { get; set; } = "All Rules";
}
