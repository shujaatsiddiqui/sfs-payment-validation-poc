namespace SfsPaymentValidation.Models;

/// <summary>
/// Response model for account enrichment
/// </summary>
public class EnrichAccountResponse
{
    /// <summary>
    /// Current account balance
    /// </summary>
    public decimal Balance { get; set; }

    /// <summary>
    /// Account status code (FIC_CLSFD_CD)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Closed to posting flag (0 = open, 1 = closed)
    /// </summary>
    public int ClosedToPosting { get; set; }

    /// <summary>
    /// Timestamp when account data was retrieved
    /// </summary>
    public DateTime EnrichedAt { get; set; }

    /// <summary>
    /// Data source identifier
    /// </summary>
    public string DataSource { get; set; } = string.Empty;
}
