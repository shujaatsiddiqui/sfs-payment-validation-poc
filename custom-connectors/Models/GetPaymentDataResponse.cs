namespace SfsPaymentValidation.Models;

/// <summary>
/// Response containing array of payment data for batch processing
/// </summary>
public class GetPaymentDataResponse
{
    /// <summary>
    /// Array of payment data objects
    /// </summary>
    public List<PaymentData> Payments { get; set; } = new();

    /// <summary>
    /// Total count of payments returned
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// When the data was retrieved
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
