# Rules Engine API

## Purpose
Internal API for managing and executing business validation rules. Called by Custom Connector API during payment validation workflow.

## Technology Stack
- .NET 8 Web API
- Microsoft.RulesEngine (for dynamic rule execution)
- Azure Blob Storage or SQL Database (for rule storage)

## Required Endpoints

### 1. GET /api/rules
**Purpose:** Get list of all available validation rules

**Response:**
```json
{
  "rules": [
    {
      "name": "ValidatePaymentData",
      "displayName": "Validate Payment Data (Rule #1)",
      "description": "Check VIN != null and Credit Amount != 0"
    },
    {
      "name": "ValidateAccountPosting",
      "displayName": "Validate Account Posting (Rule #2)",
      "description": "Check Credit Amount > 0"
    }
  ]
}
```

### 2. POST /api/validate
**Purpose:** Execute a specific rule against payment data

**Request:**
```json
{
  "ruleName": "ValidatePaymentData",
  "data": {
    "vin": "1HGCM82633A123456",
    "creditAmount": 500.00,
    "effectiveDate": "2024-11-09",
    "perfectAccountNumber": "12345"
  }
}
```

**Response:**
```json
{
  "isValid": true,
  "errorMessage": null,
  "ruleName": "ValidatePaymentData"
}
```

## Deployment

This API will be deployed as a Container App:
- **Name:** `dev-eus-ca-payment-rules-01`
- **Ingress:** Internal only (not exposed externally)
- **Called by:** Custom Connector API

## Docker Configuration

Create a `Dockerfile` in this folder:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RulesEngineApi.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RulesEngineApi.dll"]
```

## Integration with Custom Connector

The Custom Connector API (`/custom-connectors/`) will call this API:

```csharp
// In Custom Connector API
var response = await _httpClient.PostAsJsonAsync(
    "http://dev-eus-ca-payment-rules-01/api/validate",
    new { ruleName = "ValidatePaymentData", data = paymentDto }
);
```

## Next Steps

1. Create new .NET 8 Web API project in this folder
2. Implement the 2 endpoints above
3. Add Microsoft.RulesEngine NuGet package
4. Create Dockerfile
5. Test locally before deploying to Azure
