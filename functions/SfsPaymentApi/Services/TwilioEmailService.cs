using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SfsPaymentValidation.Models;

namespace SfsPaymentApi.Services;

/// <summary>
/// Service for sending emails using Twilio SendGrid API
/// </summary>
public class TwilioEmailService
{
    private readonly ILogger<TwilioEmailService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public TwilioEmailService(
        ILogger<TwilioEmailService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        // Get SendGrid configuration from environment variables
        // For now, use AuthToken as API Key (user will need to update this)
        _apiKey = configuration["Twilio:ApiKey"]
            ?? configuration["Twilio:AuthToken"]
            ?? throw new InvalidOperationException("Twilio:ApiKey or Twilio:AuthToken not configured");
        _fromEmail = configuration["Twilio:FromEmail"]
            ?? throw new InvalidOperationException("Twilio:FromEmail not configured");
        _fromName = configuration["Twilio:FromName"] ?? "SFS Payment Validation";

        // Configure HTTP client with Bearer token for SendGrid
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _logger.LogInformation("TwilioEmailService initialized for SendGrid with FromEmail: {FromEmail}", _fromEmail);
    }

    /// <summary>
    /// Sends email to one or more recipients using SendGrid API v3
    /// </summary>
    public async Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request)
    {
        try
        {
            _logger.LogInformation("Sending email to {Count} recipient(s): {Recipients}",
                request.To.Count, string.Join(", ", request.To));

            // Build SendGrid API v3 request
            var sendGridRequest = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = request.To.Select(email => new { email }).ToArray()
                    }
                },
                from = new
                {
                    email = _fromEmail,
                    name = _fromName
                },
                subject = request.Subject,
                content = new[]
                {
                    new
                    {
                        type = "text/plain",
                        value = request.TextBody ?? ExtractTextFromHtml(request.HtmlBody)
                    },
                    new
                    {
                        type = "text/html",
                        value = request.HtmlBody
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(sendGridRequest);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send request to SendGrid API v3
            var response = await _httpClient.PostAsync("https://api.sendgrid.com/v3/mail/send", httpContent);

            if (response.IsSuccessStatusCode)
            {
                // SendGrid returns 202 Accepted with X-Message-Id header
                var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                    ? values.FirstOrDefault()
                    : null;

                _logger.LogInformation("Email sent successfully via SendGrid. MessageId: {MessageId}", messageId ?? "N/A");

                return new SendEmailResponse
                {
                    Success = true,
                    RecipientCount = request.To.Count,
                    SentAt = DateTime.UtcNow,
                    Message = "Email sent successfully via SendGrid",
                    MessageId = messageId
                };
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send email via SendGrid. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorBody);

                return new SendEmailResponse
                {
                    Success = false,
                    RecipientCount = 0,
                    SentAt = DateTime.UtcNow,
                    Message = $"Failed to send email: {response.StatusCode} - {errorBody}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email");
            return new SendEmailResponse
            {
                Success = false,
                RecipientCount = 0,
                SentAt = DateTime.UtcNow,
                Message = $"Error sending email: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Extracts plain text from HTML (basic implementation)
    /// </summary>
    private string ExtractTextFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Simple HTML tag removal (for basic fallback text)
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        return text.Trim();
    }
}
