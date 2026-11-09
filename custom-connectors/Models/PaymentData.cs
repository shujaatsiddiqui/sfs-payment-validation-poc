namespace SfsPaymentValidation.Models;

/// <summary>
/// Payment data structure matching Puru Naktode DTO specifications
/// </summary>
public class PaymentData
{
    /// <summary>
    /// Payment effective date (YYYY-MM-DD format)
    /// </summary>
    public string EffectiveDate { get; set; } = string.Empty;

    /// <summary>
    /// DCBS period in YYYYMM format
    /// </summary>
    public string DcbsPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Perfect Account Number (PAN) - unique account identifier
    /// </summary>
    public string PerfectAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer short name
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Loan classification code
    /// </summary>
    public string LoanClass1 { get; set; } = string.Empty;

    /// <summary>
    /// Penalty amount
    /// </summary>
    public decimal Penalty { get; set; }

    /// <summary>
    /// NSF (Non-Sufficient Funds) fee
    /// </summary>
    public decimal Nsf { get; set; }

    /// <summary>
    /// Invoice number
    /// </summary>
    public string Invoice { get; set; } = string.Empty;

    /// <summary>
    /// Payment notes (optional)
    /// </summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle Identification Number (17 characters)
    /// </summary>
    public string Vin { get; set; } = string.Empty;

    /// <summary>
    /// Payment credit amount
    /// </summary>
    public decimal CreditAmount { get; set; }
}
