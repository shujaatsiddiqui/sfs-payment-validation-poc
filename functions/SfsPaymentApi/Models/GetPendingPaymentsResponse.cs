namespace SfsPaymentValidation.Models;

/// <summary>
/// Response containing pending payments to be processed
/// </summary>
public class GetPendingPaymentsResponse
{
    /// <summary>
    /// List of pending payments
    /// </summary>
    public List<PaymentItem> Payments { get; set; } = new();

    /// <summary>
    /// Total count of pending payments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Timestamp when payments were retrieved
    /// </summary>
    public DateTime RetrievedAt { get; set; }

    /// <summary>
    /// Data source for the payments
    /// </summary>
    public string DataSource { get; set; } = string.Empty;
}
