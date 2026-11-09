namespace SfsPaymentValidation.Models;

/// <summary>
/// Response from rule validation
/// </summary>
public class ValidateResponse
{
    /// <summary>
    /// Whether the validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Error message if validation failed (null if valid)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Name of the rule that was executed
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// When the validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
