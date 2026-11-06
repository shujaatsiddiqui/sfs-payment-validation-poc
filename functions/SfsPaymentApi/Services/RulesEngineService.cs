using Azure.Storage.Blobs;
using RulesEngine.Models;

namespace SfsPaymentApi.Services;

/// <summary>
/// Service for executing business rules using Microsoft.RulesEngine
/// Rules are loaded from Azure Blob Storage for dynamic updates without redeployment
/// </summary>
public class RulesEngineService
{
    private RulesEngine.RulesEngine _rulesEngine;
    private readonly ILogger<RulesEngineService> _logger;
    private readonly string _blobConnectionString;
    private readonly string _containerName;
    private readonly string _blobName;
    private DateTime _lastLoaded;

    public RulesEngineService(ILogger<RulesEngineService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get blob storage configuration from environment variables
        _blobConnectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString not configured");
        _containerName = configuration["BlobStorage:ContainerName"] ?? "rules";
        _blobName = configuration["BlobStorage:BlobName"] ?? "payment-validation-rules.json";

        _logger.LogInformation("RulesEngineService initialized. Container: {Container}, Blob: {Blob}",
            _containerName, _blobName);

        // Load rules synchronously during startup
        LoadRulesFromBlobAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads rules from Azure Blob Storage
    /// </summary>
    private async Task LoadRulesFromBlobAsync()
    {
        try
        {
            _logger.LogInformation("Loading rules from blob storage: {Container}/{Blob}", _containerName, _blobName);

            var blobServiceClient = new BlobServiceClient(_blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(_blobName);

            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException($"Rules blob not found: {_containerName}/{_blobName}");
            }

            var response = await blobClient.DownloadContentAsync();
            var rulesJson = response.Value.Content.ToString();

            var workflows = System.Text.Json.JsonSerializer.Deserialize<List<Workflow>>(rulesJson);

            if (workflows == null || workflows.Count == 0)
            {
                throw new InvalidOperationException("No workflows found in rules blob");
            }

            _rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray(), null);
            _lastLoaded = DateTime.UtcNow;

            _logger.LogInformation("RulesEngine loaded successfully with {Count} workflows at {Time}",
                workflows.Count, _lastLoaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading rules from blob storage");
            throw;
        }
    }

    /// <summary>
    /// Reloads rules from blob storage (call this to refresh rules without restarting)
    /// </summary>
    public async Task ReloadRulesAsync()
    {
        _logger.LogInformation("Reloading rules from blob storage");
        await LoadRulesFromBlobAsync();
    }

    /// <summary>
    /// Validates payment data against business rules
    /// </summary>
    /// <param name="paymentData">Payment validation data</param>
    /// <returns>Validation result with errors if any rules failed</returns>
    public async Task<(bool IsValid, List<string> Errors, List<string> RulesChecked)> ValidatePaymentAsync(object paymentData)
    {
        var errors = new List<string>();
        var rulesChecked = new List<string>();

        try
        {
            var results = await _rulesEngine.ExecuteAllRulesAsync("PaymentValidationRules", paymentData);

            foreach (var result in results)
            {
                rulesChecked.Add(result.Rule.RuleName);

                if (!result.IsSuccess)
                {
                    var errorMessage = result.Rule.ErrorMessage ?? $"{result.Rule.RuleName} validation failed";
                    errors.Add(errorMessage);
                    _logger.LogWarning("Rule {RuleName} failed: {ErrorMessage}", result.Rule.RuleName, errorMessage);
                }
                else
                {
                    _logger.LogDebug("Rule {RuleName} passed", result.Rule.RuleName);
                }
            }

            return (errors.Count == 0, errors, rulesChecked);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing rules engine");
            throw;
        }
    }

    /// <summary>
    /// Gets information about the currently loaded rules
    /// </summary>
    public (DateTime LastLoaded, string Container, string Blob) GetRulesInfo()
    {
        return (_lastLoaded, _containerName, _blobName);
    }
}
