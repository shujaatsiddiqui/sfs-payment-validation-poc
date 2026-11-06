namespace SfsPaymentValidation.Models;

/// <summary>
/// Response model for payment validation
/// </summary>
public class ValidationResponse
{
    /// <summary>
    /// Indicates if the payment is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
