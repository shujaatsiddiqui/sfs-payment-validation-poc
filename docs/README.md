# SFS Payment Validation - OpenAPI Specification

This directory contains the OpenAPI specification for the SFS Payment Validation custom connector.

## Files

- **sfs-connector-openapi.yaml** - OpenAPI 3.0 specification in YAML format
- **README.md** - This file

## OpenAPI Specification Overview

The specification defines a single operation:

### SfsValidatePayment

**Endpoint:** `POST /api/SfsValidatePayment`

**Purpose:** Validates payments against SFS business rules

**Business Rules:**
- **BR-005**: Zero Payment Amount - Rejects payments with CreditAmount = 0
- **BR-008**: Account Status 93 - Rejects payments where FIC_CLSFD_CD = 93

**Authentication:** Azure AD OAuth 2.0 (Managed Identity)

## Request Schema

```json
{
  "paymentId": "PAY-001",
  "ruleSelection": "All Rules"  // or "BR-005" or "BR-008"
}
```

## Response Schema

### Success (200)
```json
{
  "isValid": true,
  "errors": []
}
```

### Validation Failed (200)
```json
{
  "isValid": false,
  "errors": [
    "BR-005: Zero Payment Amount",
    "BR-008: Account status 93 blocks posting"
  ]
}
```

### Error Responses
- **400** - Bad request (missing paymentId, invalid JSON)
- **404** - Payment not found
- **500** - Internal server error

## Test Data

The function includes mock data for testing:

| Payment ID | Credit Amount | FIC_CLSFD_CD | Expected Result |
|------------|---------------|--------------|-----------------|
| PAY-001    | 1000          | 10           | ✅ Valid        |
| PAY-002    | 0             | 10           | ❌ BR-005       |
| PAY-003    | 1000          | 93           | ❌ BR-008       |
| PAY-004    | 0             | 93           | ❌ Both rules   |

## Using the OpenAPI Specification

### 1. Register Custom Connector in Azure Portal

1. Navigate to Azure Portal → Custom Connectors
2. Click "Create custom connector" → "Import an OpenAPI file"
3. Upload `sfs-connector-openapi.yaml`
4. Configure:
   - **General**: Update host to your Function App URL
   - **Security**: Configure Azure AD OAuth 2.0
   - **Definition**: Review operations (auto-populated)
   - **Code**: Optional custom code
   - **Test**: Test the connector

### 2. Update the Function App URL

Before importing, update the `servers.url` in the YAML file:

```yaml
servers:
  - url: https://your-function-app-name.azurewebsites.net
```

Or use the variable approach (already configured):

```yaml
servers:
  - url: https://{functionAppName}.azurewebsites.net
    variables:
      functionAppName:
        default: func-sfs-payment-validation
```

### 3. Configure Azure AD Authentication

The specification includes Azure AD OAuth 2.0 configuration for Managed Identity.

**Required setup:**
1. Enable Managed Identity on Logic App
2. Grant Logic App access to Function App
3. Configure Custom Connector to use Azure AD
4. Update Function App to validate Azure AD tokens

## Logic Apps Extensions

The specification includes Logic Apps-specific extensions:

- `x-ms-summary` - Friendly names in Logic Apps designer
- `x-ms-visibility` - Control field visibility (important/advanced)
- `x-ms-connector-metadata` - Connector metadata
- `x-ms-authorization-url-template` - Azure AD auth URLs

## Next Steps

1. ✅ OpenAPI spec created
2. 🔲 Test function locally (Task #4)
3. 🔲 Deploy infrastructure (Task #5-6)
4. 🔲 Register custom connector (Task #8)
5. 🔲 Create Logic App workflow (Task #9)
6. 🔲 Test end-to-end (Task #10)
