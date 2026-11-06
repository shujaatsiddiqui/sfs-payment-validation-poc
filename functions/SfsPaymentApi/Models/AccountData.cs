namespace SfsPaymentValidation.Models;

/// <summary>
/// Internal model for account data (used in mock database)
/// </summary>
public class AccountData
{
    public decimal Balance { get; set; }
    public int Status { get; set; }
    public int ClosedToPosting { get; set; }
}
