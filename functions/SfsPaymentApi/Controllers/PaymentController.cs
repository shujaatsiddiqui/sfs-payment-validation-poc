using Microsoft.AspNetCore.Mvc;
using SfsPaymentValidation.Models;

namespace SfsPaymentApi.Controllers;

[ApiController]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;

    // Mock posted payments database
    private static readonly HashSet<string> PostedPayments = new()
    {
        "PAY-001:1000",
        "PAY-005:500"
    };

    // Mock account database
    private static readonly Dictionary<string, AccountData> MockAccounts = new()
    {
        { "PAY-001", new AccountData { Balance = 1000.00m, Status = 10, ClosedToPosting = 0 } },
        { "PAY-002", new AccountData { Balance = 500.00m, Status = 10, ClosedToPosting = 0 } },
        { "PAY-003", new AccountData { Balance = 1000.00m, Status = 93, ClosedToPosting = 0 } },
        { "PAY-004", new AccountData { Balance = 100.00m, Status = 10, ClosedToPosting = 1 } },
        { "PAY-006", new AccountData { Balance = 0.00m, Status = 10, ClosedToPosting = 0 } }
    };

    // Mock payment database for legacy function
    private static readonly Dictionary<string, Payment> MockPayments = new()
    {
        { "PAY-001", new Payment { PaymentId = "PAY-001", CreditAmount = 1000, FIC_CLSFD_CD = 10, AccountNumber = "ACC-001" } },
        { "PAY-002", new Payment { PaymentId = "PAY-002", CreditAmount = 0, FIC_CLSFD_CD = 10, AccountNumber = "ACC-002" } },
        { "PAY-003", new Payment { PaymentId = "PAY-003", CreditAmount = 1000, FIC_CLSFD_CD = 93, AccountNumber = "ACC-003" } },
        { "PAY-004", new Payment { PaymentId = "PAY-004", CreditAmount = 0, FIC_CLSFD_CD = 93, AccountNumber = "ACC-004" } }
    };

    public PaymentController(ILogger<PaymentController> logger)
    {
        _logger = logger;
    }

    [HttpPost("CheckDuplicate")]
    public IActionResult CheckDuplicate([FromBody] DuplicateCheckRequest request)
    {
        _logger.LogInformation("CheckDuplicate: {PaymentId}", request.PaymentId);

        var compositeKey = $"{request.PaymentId}:{request.Amount}";
        var isDuplicate = PostedPayments.Contains(compositeKey);

        return Ok(new DuplicateCheckResponse
        {
            IsDuplicate = isDuplicate,
            Message = isDuplicate
                ? $"Payment {request.PaymentId} with amount {request.Amount} already posted"
                : $"Payment {request.PaymentId} with amount {request.Amount} is unique",
            CompositeKey = compositeKey,
            CheckedAt = DateTime.UtcNow
        });
    }

    [HttpPost("EnrichAccount")]
    public IActionResult EnrichAccount([FromBody] EnrichAccountRequest request)
    {
        _logger.LogInformation("EnrichAccount: {PaymentId}", request.PaymentId);

        var accountData = MockAccounts.GetValueOrDefault(
            request.PaymentId,
            new AccountData { Balance = 1000.00m, Status = 10, ClosedToPosting = 0 }
        );

        return Ok(new EnrichAccountResponse
        {
            Balance = accountData.Balance,
            Status = accountData.Status,
            ClosedToPosting = accountData.ClosedToPosting,
            EnrichedAt = DateTime.UtcNow,
            DataSource = "StudentAccountDB"
        });
    }

    [HttpPost("ValidatePayment")]
    public IActionResult ValidatePayment([FromBody] PaymentValidationRequest request)
    {
        _logger.LogInformation("ValidatePayment: {PaymentId}", request.PaymentId);

        var errors = new List<string>();

        // BR-001: Balance must be positive
        if (request.Balance <= 0)
        {
            errors.Add("Account balance is zero or negative");
        }

        // BR-002: Status 93 blocks posting
        if (request.Status == 93)
        {
            errors.Add("Account status 93 blocks posting");
        }

        // BR-003: Account must be open to posting
        if (request.ClosedToPosting == 1)
        {
            errors.Add("Account is closed to posting");
        }

        // BR-004: No overpayments allowed
        if (request.Amount > request.Balance)
        {
            errors.Add("Payment amount exceeds account balance");
        }

        // Classify payment type
        var paymentType = request.Amount < request.Balance ? "Standard"
            : request.Amount == request.Balance ? "Payoff"
            : "Overpayment";

        return Ok(new PaymentValidationResponse
        {
            IsValid = errors.Count == 0,
            PaymentType = paymentType,
            Errors = errors,
            RulesChecked = new List<string> { "BR-001", "BR-002", "BR-003", "BR-004" },
            ValidatedAt = DateTime.UtcNow
        });
    }

    [HttpPost("SfsValidatePayment")]
    public IActionResult SfsValidatePayment([FromBody] ValidationRequest request)
    {
        _logger.LogInformation("SfsValidatePayment: {PaymentId}", request.PaymentId);

        if (!MockPayments.TryGetValue(request.PaymentId, out var payment))
        {
            return NotFound(new { error = $"Payment {request.PaymentId} not found" });
        }

        var response = new ValidationResponse { IsValid = true };

        // BR-005: Zero Payment Amount
        if (request.RuleSelection is "All Rules" or "BR-005")
        {
            if (payment.CreditAmount == 0)
            {
                response.Errors.Add("BR-005: Zero Payment Amount");
                response.IsValid = false;
            }
        }

        // BR-008: Account status 93 blocks posting
        if (request.RuleSelection is "All Rules" or "BR-008")
        {
            if (payment.FIC_CLSFD_CD == 93)
            {
                response.Errors.Add("BR-008: Account status 93 blocks posting");
                response.IsValid = false;
            }
        }

        return Ok(response);
    }
}
