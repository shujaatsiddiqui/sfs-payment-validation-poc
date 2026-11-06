using Microsoft.AspNetCore.Mvc;
using SfsPaymentValidation.Models;
using SfsPaymentApi.Services;

namespace SfsPaymentApi.Controllers;

[ApiController]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly RulesEngineService _rulesEngineService;

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

    // Mock pending payments queue
    private static readonly List<PaymentItem> PendingPayments = new()
    {
        new PaymentItem { Id = "PAY-001", Amount = 1000 },
        new PaymentItem { Id = "PAY-002", Amount = 500 },
        new PaymentItem { Id = "PAY-003", Amount = 800 },
        new PaymentItem { Id = "PAY-004", Amount = 50 },
        new PaymentItem { Id = "PAY-006", Amount = 100 }
    };

    public PaymentController(ILogger<PaymentController> logger, RulesEngineService rulesEngineService)
    {
        _logger = logger;
        _rulesEngineService = rulesEngineService;
    }

    /// <summary>
    /// Retrieves pending payments that need to be processed.
    /// </summary>
    /// <remarks>
    /// This endpoint returns a list of payments waiting to be validated and posted.
    /// The Logic App workflow calls this to get payments for batch processing.
    ///
    /// What it does:
    /// - Fetches pending payments from the payment queue/database
    /// - Returns payment ID and amount for each pending payment
    /// - Logic App then processes each payment through the validation workflow
    ///
    /// Workflow Integration:
    /// 1. Logic App calls GetPendingPayments
    /// 2. Receives array of payments
    /// 3. Loops through each payment
    /// 4. For each payment: CheckDuplicate → EnrichAccount → ValidatePayment
    ///
    /// Example Request:
    /// GET /api/GetPendingPayments
    ///
    /// Example Response:
    /// {
    ///   "payments": [
    ///     {"id": "PAY-001", "amount": 1000},
    ///     {"id": "PAY-002", "amount": 500},
    ///     {"id": "PAY-003", "amount": 800},
    ///     {"id": "PAY-004", "amount": 50},
    ///     {"id": "PAY-006", "amount": 100}
    ///   ],
    ///   "totalCount": 5,
    ///   "retrievedAt": "2026-11-06T08:00:00.000Z",
    ///   "dataSource": "PaymentQueueDB"
    /// }
    ///
    /// Note: In production, this would query an actual database or message queue.
    /// </remarks>
    /// <returns>GetPendingPaymentsResponse with list of pending payments</returns>
    [HttpGet("GetPendingPayments")]
    public IActionResult GetPendingPayments()
    {
        _logger.LogInformation("GetPendingPayments called - retrieving pending payments");

        return Ok(new GetPendingPaymentsResponse
        {
            Payments = PendingPayments,
            TotalCount = PendingPayments.Count,
            RetrievedAt = DateTime.UtcNow,
            DataSource = "PaymentQueueDB"
        });
    }

    /// <summary>
    /// Checks if a payment has already been posted to prevent duplicate processing.
    /// </summary>
    /// <remarks>
    /// This endpoint detects duplicate payments using a composite key (paymentId:amount).
    /// Called first in the workflow to skip payments that have already been processed.
    ///
    /// Example Request:
    /// POST /api/CheckDuplicate
    /// {
    ///   "paymentId": "PAY-001",
    ///   "amount": 1000
    /// }
    ///
    /// Example Response (Duplicate Found):
    /// {
    ///   "isDuplicate": true,
    ///   "message": "Payment PAY-001 with amount 1000 already posted",
    ///   "compositeKey": "PAY-001:1000",
    ///   "checkedAt": "2026-11-06T07:30:50.363Z"
    /// }
    ///
    /// Example Response (Not Duplicate):
    /// {
    ///   "isDuplicate": false,
    ///   "message": "Payment PAY-001 with amount 500 is unique",
    ///   "compositeKey": "PAY-001:500",
    ///   "checkedAt": "2026-11-06T07:30:50.363Z"
    /// }
    /// </remarks>
    /// <param name="request">Contains paymentId and amount to check</param>
    /// <returns>DuplicateCheckResponse indicating if payment is duplicate</returns>
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

    /// <summary>
    /// Retrieves current account information from the student financial system.
    /// </summary>
    /// <remarks>
    /// This endpoint enriches payment data with account details needed for validation.
    /// Called after duplicate check to gather account balance, status, and posting flags.
    ///
    /// What it does:
    /// - Looks up student account by payment ID
    /// - Returns current balance, account status code, and closed-to-posting flag
    /// - Data source: Student Account Database (StudentAccountDB)
    ///
    /// Field Descriptions:
    /// - balance: Current account balance in dollars
    /// - status: Account status code (10 = Active, 93 = Blocked for posting)
    /// - closedToPosting: Flag indicating if account accepts new postings (0 = Open, 1 = Closed)
    ///
    /// Example Request:
    /// POST /api/EnrichAccount
    /// {
    ///   "paymentId": "PAY-002"
    /// }
    ///
    /// Example Response:
    /// {
    ///   "balance": 500.00,
    ///   "status": 10,
    ///   "closedToPosting": 0,
    ///   "enrichedAt": "2026-11-06T07:30:53.593Z",
    ///   "dataSource": "StudentAccountDB"
    /// }
    /// </remarks>
    /// <param name="request">Contains paymentId to lookup account data</param>
    /// <returns>EnrichAccountResponse with account balance, status, and posting flag</returns>
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

    /// <summary>
    /// Validates payment against comprehensive business rules (BR-001 through BR-004).
    /// </summary>
    /// <remarks>
    /// This endpoint applies business rules to validate if a payment can be posted.
    /// Called after account enrichment to determine final payment approval.
    ///
    /// Uses Microsoft.RulesEngine for JSON-based rule evaluation from rules.json.
    ///
    /// Business Rules Checked:
    /// - BR-001: Balance must be positive (balance > 0)
    /// - BR-002: Account status 93 blocks posting
    /// - BR-003: Account must be open to posting (closedToPosting = 0)
    /// - BR-004: No overpayments allowed (amount ≤ balance)
    ///
    /// Payment Classification:
    /// - Standard: Payment amount &lt; account balance
    /// - Payoff: Payment amount = account balance (pays off entire balance)
    /// - Overpayment: Payment amount > account balance
    ///
    /// Example Request:
    /// POST /api/ValidatePayment
    /// {
    ///   "paymentId": "PAY-002",
    ///   "amount": 500,
    ///   "balance": 500.00,
    ///   "status": 10,
    ///   "closedToPosting": 0
    /// }
    ///
    /// Example Response (Valid - Payoff):
    /// {
    ///   "isValid": true,
    ///   "paymentType": "Payoff",
    ///   "errors": [],
    ///   "rulesChecked": ["BR-001", "BR-002", "BR-003", "BR-004"],
    ///   "validatedAt": "2026-11-06T08:00:00.000Z"
    /// }
    ///
    /// Example Response (Invalid - Multiple Violations):
    /// {
    ///   "isValid": false,
    ///   "paymentType": "Overpayment",
    ///   "errors": [
    ///     "Account balance is zero or negative",
    ///     "Payment amount exceeds account balance"
    ///   ],
    ///   "rulesChecked": ["BR-001", "BR-002", "BR-003", "BR-004"],
    ///   "validatedAt": "2026-11-06T08:00:00.000Z"
    /// }
    /// </remarks>
    /// <param name="request">Contains payment details and enriched account data</param>
    /// <returns>PaymentValidationResponse indicating if payment is valid and its classification</returns>
    [HttpPost("ValidatePayment")]
    public async Task<IActionResult> ValidatePayment([FromBody] PaymentValidationRequest request)
    {
        _logger.LogInformation("ValidatePayment: {PaymentId}", request.PaymentId);

        // Create input object for rules engine with lowercase property names (matching rules.json expressions)
        var paymentData = new
        {
            paymentId = request.PaymentId,
            amount = request.Amount,
            balance = request.Balance,
            status = request.Status,
            closedToPosting = request.ClosedToPosting
        };

        // Execute rules using RulesEngine
        var (isValid, errors, rulesChecked) = await _rulesEngineService.ValidatePaymentAsync(paymentData);

        // Classify payment type
        var paymentType = request.Amount < request.Balance ? "Standard"
            : request.Amount == request.Balance ? "Payoff"
            : "Overpayment";

        return Ok(new PaymentValidationResponse
        {
            IsValid = isValid,
            PaymentType = paymentType,
            Errors = errors,
            RulesChecked = rulesChecked,
            ValidatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Legacy validation endpoint with selective rule execution support.
    /// </summary>
    /// <remarks>
    /// This endpoint validates payments using a subset of business rules based on rule selection.
    /// Supports legacy systems that require specific rule validation.
    ///
    /// Business Rules:
    /// - BR-005: Zero Payment Amount (creditAmount must be > 0)
    /// - BR-008: Account status 93 blocks posting
    ///
    /// Rule Selection Options:
    /// - "All Rules": Checks all applicable rules (BR-005 and BR-008)
    /// - "BR-005": Checks only zero payment amount rule
    /// - "BR-008": Checks only status 93 rule
    ///
    /// Note: Payment data is looked up from internal database by paymentId.
    ///
    /// Example Request:
    /// POST /api/SfsValidatePayment
    /// {
    ///   "paymentId": "PAY-001",
    ///   "ruleSelection": "All Rules"
    /// }
    ///
    /// Example Response (Valid):
    /// {
    ///   "isValid": true,
    ///   "errors": []
    /// }
    ///
    /// Example Response (Invalid - Zero Amount):
    /// {
    ///   "isValid": false,
    ///   "errors": ["BR-005: Zero Payment Amount"]
    /// }
    ///
    /// Example Response (Invalid - Status 93):
    /// {
    ///   "isValid": false,
    ///   "errors": ["BR-008: Account status 93 blocks posting"]
    /// }
    ///
    /// Example Response (Not Found):
    /// {
    ///   "error": "Payment PAY-999 not found"
    /// }
    /// </remarks>
    /// <param name="request">Contains paymentId and rule selection criteria</param>
    /// <returns>ValidationResponse with validation result and any rule violations</returns>
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
