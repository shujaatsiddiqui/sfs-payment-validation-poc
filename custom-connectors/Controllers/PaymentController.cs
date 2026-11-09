using Microsoft.AspNetCore.Mvc;
using SfsPaymentValidation.Models;

namespace SfsPaymentApi.Controllers;

[ApiController]
[Route("api")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    // Mock payment data matching Puru Naktode DTO structure
    private static readonly List<PaymentData> MockPayments = new()
    {
        new PaymentData
        {
            EffectiveDate = "2024-11-09",
            DcbsPeriod = "202411",
            PerfectAccountNumber = "12345",
            ShortName = "John Doe",
            LoanClass1 = "AUTO",
            Penalty = 0,
            Nsf = 0,
            Invoice = "INV-001",
            Notes = "",
            Vin = "1HGCM82633A123456",
            CreditAmount = 500.00m
        },
        new PaymentData
        {
            EffectiveDate = "2024-11-09",
            DcbsPeriod = "202411",
            PerfectAccountNumber = "67890",
            ShortName = "Jane Smith",
            LoanClass1 = "LEASE",
            Penalty = 25.00m,
            Nsf = 35.00m,
            Invoice = "INV-002",
            Notes = "Late payment",
            Vin = "2HGCM82633A789012",
            CreditAmount = 750.00m
        },
        new PaymentData
        {
            EffectiveDate = "2024-11-08",
            DcbsPeriod = "202411",
            PerfectAccountNumber = "11111",
            ShortName = "Bob Johnson",
            LoanClass1 = "AUTO",
            Penalty = 0,
            Nsf = 0,
            Invoice = "INV-003",
            Notes = "",
            Vin = "", // Invalid - null VIN
            CreditAmount = 0.00m // Invalid - zero amount
        },
        new PaymentData
        {
            EffectiveDate = "2024-11-07",
            DcbsPeriod = "202411",
            PerfectAccountNumber = "22222",
            ShortName = "Alice Brown",
            LoanClass1 = "LEASE",
            Penalty = 0,
            Nsf = 0,
            Invoice = "INV-004",
            Notes = "",
            Vin = "3HGCM82633A345678",
            CreditAmount = 1200.00m
        }
    };

    public PaymentController(
        ILogger<PaymentController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Get Available Validation Rules
    /// </summary>
    /// <remarks>
    /// Retrieves list of all available business rules from Rules Engine API.
    /// Used to populate dropdown in Logic App designer for dynamic rule selection.
    ///
    /// This endpoint calls the internal Rules Engine API to get the current list of rules.
    ///
    /// Example Response:
    /// {
    ///   "rules": [
    ///     {
    ///       "name": "ValidatePaymentData",
    ///       "displayName": "Validate Payment Data (Rule #1)",
    ///       "description": "Check VIN != null and Credit Amount != 0"
    ///     },
    ///     {
    ///       "name": "ValidateAccountPosting",
    ///       "displayName": "Validate Account Posting (Rule #2)",
    ///       "description": "Check Credit Amount > 0"
    ///     }
    ///   ]
    /// }
    /// </remarks>
    /// <returns>GetRulesResponse with list of available rules</returns>
    [HttpGet("Rules")]
    [ProducesResponseType(typeof(GetRulesResponse), 200)]
    public async Task<IActionResult> GetRules()
    {
        _logger.LogInformation("GetRules called - retrieving available validation rules");

        try
        {
            // Call Rules Engine API to get available rules
            var rulesEngineUrl = _configuration["RulesEngineApi:Url"] ?? "http://dev-eus-ca-payment-rules-01";
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync($"{rulesEngineUrl}/api/rules");

            if (response.IsSuccessStatusCode)
            {
                var rulesResponse = await response.Content.ReadFromJsonAsync<GetRulesResponse>();
                return Ok(rulesResponse);
            }
            else
            {
                // Fallback to mock data if Rules Engine API is not available
                _logger.LogWarning("Rules Engine API not available, returning mock data");
                return Ok(new GetRulesResponse
                {
                    Rules = new List<RuleInfo>
                    {
                        new RuleInfo
                        {
                            Name = "ValidatePaymentData",
                            DisplayName = "Validate Payment Data (Rule #1)",
                            Description = "Check VIN != null and Credit Amount != 0"
                        },
                        new RuleInfo
                        {
                            Name = "ValidateAccountPosting",
                            DisplayName = "Validate Account Posting (Rule #2)",
                            Description = "Check Credit Amount > 0"
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rules from Rules Engine API");
            // Return mock data on error
            return Ok(new GetRulesResponse
            {
                Rules = new List<RuleInfo>
                {
                    new RuleInfo
                    {
                        Name = "ValidatePaymentData",
                        DisplayName = "Validate Payment Data (Rule #1)",
                        Description = "Check VIN != null and Credit Amount != 0"
                    },
                    new RuleInfo
                    {
                        Name = "ValidateAccountPosting",
                        DisplayName = "Validate Account Posting (Rule #2)",
                        Description = "Check Credit Amount > 0"
                    }
                }
            });
        }
    }

    /// <summary>
    /// Validate Payment Against Rule
    /// </summary>
    /// <remarks>
    /// Executes validation using selected business rule from Rules Engine.
    /// Generic endpoint that can validate against any rule by name.
    ///
    /// This endpoint calls the internal Rules Engine API to execute the specified rule.
    ///
    /// Example Request:
    /// POST /api/Validate
    /// {
    ///   "ruleName": "ValidatePaymentData",
    ///   "data": {
    ///     "vin": "1HGCM82633A123456",
    ///     "creditAmount": 500.00,
    ///     "effectiveDate": "2024-11-09"
    ///   }
    /// }
    ///
    /// Example Response (Valid):
    /// {
    ///   "isValid": true,
    ///   "errorMessage": null,
    ///   "ruleName": "ValidatePaymentData",
    ///   "validatedAt": "2024-11-09T12:00:00Z"
    /// }
    ///
    /// Example Response (Invalid):
    /// {
    ///   "isValid": false,
    ///   "errorMessage": "VIN is null or Credit Amount is 0",
    ///   "ruleName": "ValidatePaymentData",
    ///   "validatedAt": "2024-11-09T12:00:00Z"
    /// }
    /// </remarks>
    /// <param name="request">Contains ruleName and payment data</param>
    /// <returns>ValidateResponse with validation result</returns>
    [HttpPost("Validate")]
    [ProducesResponseType(typeof(ValidateResponse), 200)]
    public async Task<IActionResult> Validate([FromBody] ValidateRequest request)
    {
        _logger.LogInformation("Validate called with rule: {RuleName}", request.RuleName);

        try
        {
            // Call Rules Engine API to execute validation
            var rulesEngineUrl = _configuration["RulesEngineApi:Url"] ?? "http://dev-eus-ca-payment-rules-01";
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.PostAsJsonAsync($"{rulesEngineUrl}/api/validate", request);

            if (response.IsSuccessStatusCode)
            {
                var validateResponse = await response.Content.ReadFromJsonAsync<ValidateResponse>();
                return Ok(validateResponse);
            }
            else
            {
                // Fallback to mock validation if Rules Engine API is not available
                _logger.LogWarning("Rules Engine API not available, performing mock validation");
                return Ok(PerformMockValidation(request));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Rules Engine API for validation");
            // Return mock validation on error
            return Ok(PerformMockValidation(request));
        }
    }

    /// <summary>
    /// Get Payment Data for Validation
    /// </summary>
    /// <remarks>
    /// Retrieves array of payment data for batch processing.
    /// Called by Logic App workflow to get payments that need validation.
    ///
    /// Returns payment data matching Puru Naktode DTO structure.
    ///
    /// Example Response:
    /// {
    ///   "payments": [
    ///     {
    ///       "effectiveDate": "2024-11-09",
    ///       "dcbsPeriod": "202411",
    ///       "perfectAccountNumber": "12345",
    ///       "shortName": "John Doe",
    ///       "loanClass1": "AUTO",
    ///       "penalty": 0,
    ///       "nsf": 0,
    ///       "invoice": "INV-001",
    ///       "notes": "",
    ///       "vin": "1HGCM82633A123456",
    ///       "creditAmount": 500.00
    ///     }
    ///   ],
    ///   "totalCount": 4,
    ///   "retrievedAt": "2024-11-09T12:00:00Z"
    /// }
    /// </remarks>
    /// <returns>GetPaymentDataResponse with array of payment data</returns>
    [HttpGet("GetPaymentData")]
    [ProducesResponseType(typeof(GetPaymentDataResponse), 200)]
    public IActionResult GetPaymentData()
    {
        _logger.LogInformation("GetPaymentData called - retrieving payment data for batch processing");

        return Ok(new GetPaymentDataResponse
        {
            Payments = MockPayments,
            TotalCount = MockPayments.Count,
            RetrievedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Post to Salesforce Unposted Queue
    /// </summary>
    /// <remarks>
    /// Posts invalid payment to Salesforce unposted queue for manual review.
    /// Called when payment fails validation.
    ///
    /// Returns dummy success response (no actual Salesforce integration).
    ///
    /// Example Request:
    /// POST /api/PostToSalesforce
    /// {
    ///   "vin": "1HGCM82633A123456",
    ///   "creditAmount": 500.00,
    ///   ...
    /// }
    ///
    /// Example Response:
    /// {
    ///   "success": true,
    ///   "queueId": "SF-12345-20241109",
    ///   "message": "Payment posted to Salesforce unposted queue successfully",
    ///   "postedAt": "2024-11-09T12:00:00Z"
    /// }
    /// </remarks>
    /// <param name="paymentData">Payment data to post</param>
    /// <returns>SalesforceResponse with queue ID</returns>
    [HttpPost("PostToSalesforce")]
    [ProducesResponseType(typeof(SalesforceResponse), 200)]
    public IActionResult PostToSalesforce([FromBody] PaymentData paymentData)
    {
        _logger.LogInformation("PostToSalesforce called for account: {AccountNumber}", paymentData.PerfectAccountNumber);

        // Mock Salesforce posting - return success
        var queueId = $"SF-{Random.Shared.Next(10000, 99999)}-{DateTime.UtcNow:yyyyMMdd}";

        return Ok(new SalesforceResponse
        {
            Success = true,
            QueueId = queueId,
            Message = "Payment posted to Salesforce unposted queue successfully",
            PostedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Post to LM Servicing System
    /// </summary>
    /// <remarks>
    /// Posts validated payment to Loan Management servicing system for processing.
    /// Called when payment passes all validations.
    ///
    /// Returns dummy success response (no actual LM integration).
    ///
    /// Example Request:
    /// POST /api/PostToLMServicing
    /// {
    ///   "vin": "1HGCM82633A123456",
    ///   "creditAmount": 500.00,
    ///   ...
    /// }
    ///
    /// Example Response:
    /// {
    ///   "success": true,
    ///   "transactionId": "LM-67890-20241109",
    ///   "message": "Payment posted to LM servicing successfully",
    ///   "postedAt": "2024-11-09T12:00:00Z"
    /// }
    /// </remarks>
    /// <param name="paymentData">Validated payment data to post</param>
    /// <returns>LMServicingResponse with transaction ID</returns>
    [HttpPost("PostToLMServicing")]
    [ProducesResponseType(typeof(LMServicingResponse), 200)]
    public IActionResult PostToLMServicing([FromBody] PaymentData paymentData)
    {
        _logger.LogInformation("PostToLMServicing called for account: {AccountNumber}", paymentData.PerfectAccountNumber);

        // Mock LM Servicing posting - return success
        var transactionId = $"LM-{Random.Shared.Next(10000, 99999)}-{DateTime.UtcNow:yyyyMMdd}";

        return Ok(new LMServicingResponse
        {
            Success = true,
            TransactionId = transactionId,
            Message = "Payment posted to LM servicing successfully",
            PostedAt = DateTime.UtcNow
        });
    }

    // Helper method for mock validation when Rules Engine API is not available
    private ValidateResponse PerformMockValidation(ValidateRequest request)
    {
        var data = request.Data;
        bool isValid = true;
        string? errorMessage = null;

        switch (request.RuleName)
        {
            case "ValidatePaymentData":
                // Rule #1: VIN != null AND Credit Amount != 0
                if (string.IsNullOrEmpty(data.Vin) || data.CreditAmount == 0)
                {
                    isValid = false;
                    errorMessage = "VIN is null or Credit Amount is 0";
                }
                break;

            case "ValidateAccountPosting":
                // Rule #2: Credit Amount > 0
                if (data.CreditAmount <= 0)
                {
                    isValid = false;
                    errorMessage = "Credit Amount must be greater than 0";
                }
                break;

            default:
                _logger.LogWarning("Unknown rule name: {RuleName}", request.RuleName);
                isValid = false;
                errorMessage = $"Unknown rule: {request.RuleName}";
                break;
        }

        return new ValidateResponse
        {
            IsValid = isValid,
            ErrorMessage = errorMessage,
            RuleName = request.RuleName,
            ValidatedAt = DateTime.UtcNow
        };
    }
}
