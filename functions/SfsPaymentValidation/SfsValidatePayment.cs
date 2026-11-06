using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;
using SfsPaymentValidation.Models;

namespace SfsPaymentValidation;

public class SfsValidatePayment
{
    private readonly ILogger<SfsValidatePayment> _logger;

    // Mock database of payments for testing
    private static readonly Dictionary<string, Payment> MockPayments = new()
    {
        { "PAY-001", new Payment { PaymentId = "PAY-001", CreditAmount = 1000, FIC_CLSFD_CD = 10, AccountNumber = "ACC-001" } },
        { "PAY-002", new Payment { PaymentId = "PAY-002", CreditAmount = 0, FIC_CLSFD_CD = 10, AccountNumber = "ACC-002" } },
        { "PAY-003", new Payment { PaymentId = "PAY-003", CreditAmount = 1000, FIC_CLSFD_CD = 93, AccountNumber = "ACC-003" } },
        { "PAY-004", new Payment { PaymentId = "PAY-004", CreditAmount = 0, FIC_CLSFD_CD = 93, AccountNumber = "ACC-004" } }
    };

    public SfsValidatePayment(ILogger<SfsValidatePayment> logger)
    {
        _logger = logger;
    }

    [Function("SfsValidatePayment")]
    [OpenApiOperation(operationId: "SfsValidatePayment", tags: new[] { "Payment Validation" }, Summary = "Validate SFS Payment", Description = "Validates a payment against SFS business rules (BR-005 Zero Payment Amount, BR-008 Account Status 93)", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ValidationRequest), Required = true, Description = "Payment validation request containing payment ID and rule selection")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ValidationResponse), Summary = "Validation completed successfully", Description = "Returns validation result with isValid flag and list of errors")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Bad request", Description = "Invalid input - PaymentId is required")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Payment not found", Description = "The specified payment ID does not exist")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(ErrorResponse), Summary = "Internal server error", Description = "An error occurred processing the request")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("SFS Payment Validation request received");

        try
        {
            // Read and deserialize request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var validationRequest = JsonSerializer.Deserialize<ValidationRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (validationRequest == null || string.IsNullOrEmpty(validationRequest.PaymentId))
            {
                _logger.LogWarning("Invalid request: PaymentId is required");
                return new BadRequestObjectResult(new { error = "PaymentId is required" });
            }

            _logger.LogInformation("Validating PaymentId: {PaymentId} with RuleSelection: {RuleSelection}",
                validationRequest.PaymentId, validationRequest.RuleSelection);

            // Lookup payment in mock database
            if (!MockPayments.TryGetValue(validationRequest.PaymentId, out var payment))
            {
                _logger.LogWarning("Payment not found: {PaymentId}", validationRequest.PaymentId);
                return new NotFoundObjectResult(new { error = $"Payment {validationRequest.PaymentId} not found" });
            }

            // Perform validation
            var validationResponse = ValidatePayment(payment, validationRequest.RuleSelection);

            _logger.LogInformation("Validation completed for {PaymentId}: IsValid={IsValid}, ErrorCount={ErrorCount}",
                validationRequest.PaymentId, validationResponse.IsValid, validationResponse.Errors.Count);

            return new OkObjectResult(validationResponse);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in request body");
            return new BadRequestObjectResult(new { error = "Invalid JSON format" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing validation request");
            return new ObjectResult(new { error = "Internal server error" }) { StatusCode = 500 };
        }
    }

    private ValidationResponse ValidatePayment(Payment payment, string ruleSelection)
    {
        var response = new ValidationResponse { IsValid = true };

        // BR-005: Zero Payment Amount
        if (ruleSelection is "All Rules" or "BR-005")
        {
            if (payment.CreditAmount == 0)
            {
                response.Errors.Add("BR-005: Zero Payment Amount");
                response.IsValid = false;
                _logger.LogInformation("Payment {PaymentId} failed BR-005: Zero Payment Amount", payment.PaymentId);
            }
        }

        // BR-008: Account status 93 blocks posting
        if (ruleSelection is "All Rules" or "BR-008")
        {
            if (payment.FIC_CLSFD_CD == 93)
            {
                response.Errors.Add("BR-008: Account status 93 blocks posting");
                response.IsValid = false;
                _logger.LogInformation("Payment {PaymentId} failed BR-008: Account status 93", payment.PaymentId);
            }
        }

        return response;
    }
}
