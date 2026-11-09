namespace SfsPaymentValidation.Models;

/// <summary>
/// Information about an available validation rule
/// </summary>
public class RuleInfo
{
    /// <summary>
    /// Rule identifier (technical name) - used in API calls
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule display name shown in UI/dropdown
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Rule description explaining what it validates
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
