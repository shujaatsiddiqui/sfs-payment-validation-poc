namespace SfsPaymentValidation.Models;

/// <summary>
/// Response containing list of available validation rules
/// </summary>
public class GetRulesResponse
{
    /// <summary>
    /// List of available rules
    /// </summary>
    public List<RuleInfo> Rules { get; set; } = new();
}
