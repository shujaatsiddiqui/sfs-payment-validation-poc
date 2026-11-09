namespace SfsPaymentValidation.Models;

/// <summary>
/// Request to validate payment data against a specific rule
/// </summary>
public class ValidateRequest
{
    /// <summary>
    /// Name of the rule to execute (e.g., "ValidatePaymentData", "ValidateAccountPosting")
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Payment data to validate
    /// </summary>
    public PaymentData Data { get; set; } = new();
}
