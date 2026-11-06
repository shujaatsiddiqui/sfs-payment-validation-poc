namespace SfsPaymentValidation.Models;

/// <summary>
/// Payment entity model
/// </summary>
public class Payment
{
    /// <summary>
    /// Payment identifier
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Credit amount for the payment
    /// </summary>
    public decimal CreditAmount { get; set; }

    /// <summary>
    /// FIC Classification Code (Account Status)
    /// </summary>
    public int FIC_CLSFD_CD { get; set; }

    /// <summary>
    /// Account number
    /// </summary>
    public string AccountNumber { get; set; } = string.Empty;
}
