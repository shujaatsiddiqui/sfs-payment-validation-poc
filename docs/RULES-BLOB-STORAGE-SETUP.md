# Rules Engine - Blob Storage Setup

This document explains how to configure the payment validation rules in Azure Blob Storage.

## Overview

The payment validation rules are stored in Azure Blob Storage instead of being embedded in the container. This allows updating business rules without redeploying the application.

## Benefits

- **Dynamic Updates**: Change rules without redeploying the container
- **Centralized Management**: Rules stored in Azure Blob Storage
- **Version Control**: Can version rules in blob storage
- **Hot Reload**: Optional reload endpoint (future enhancement)

## Setup Instructions

### 1. Create Storage Account and Container

```bash
# Variables
RESOURCE_GROUP="rg-sfs-payment-validation"
STORAGE_ACCOUNT="stsfspaymentrules"  # Must be globally unique, lowercase, no hyphens
LOCATION="centralus"
CONTAINER_NAME="rules"

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

# Get connection string
CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString -o tsv)

echo "Connection String: $CONNECTION_STRING"

# Create container
az storage container create \
  --name $CONTAINER_NAME \
  --connection-string "$CONNECTION_STRING"
```

### 2. Upload Rules File

```bash
# Upload the rules template
az storage blob upload \
  --container-name $CONTAINER_NAME \
  --name payment-validation-rules.json \
  --file docs/templates/payment-validation-rules.json \
  --connection-string "$CONNECTION_STRING"

# Verify upload
az storage blob list \
  --container-name $CONTAINER_NAME \
  --connection-string "$CONNECTION_STRING" \
  --output table
```

### 3. Configure Container App Environment Variables

```bash
# Get the container app name
CONTAINER_APP="ca-sfs-payment-validation"

# Update environment variables
az containerapp update \
  --name $CONTAINER_APP \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    "BlobStorage__ConnectionString=$CONNECTION_STRING" \
    "BlobStorage__ContainerName=$CONTAINER_NAME" \
    "BlobStorage__BlobName=payment-validation-rules.json"
```

**Note**: The double underscore (`__`) is required for nested configuration in ASP.NET Core.

### 4. Alternative - Use Managed Identity (Recommended for Production)

Instead of connection string, use Managed Identity:

```bash
# Enable system-assigned managed identity
az containerapp identity assign \
  --name $CONTAINER_APP \
  --resource-group $RESOURCE_GROUP

# Get the identity principal ID
PRINCIPAL_ID=$(az containerapp identity show \
  --name $CONTAINER_APP \
  --resource-group $RESOURCE_GROUP \
  --query principalId -o tsv)

# Grant Storage Blob Data Reader role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"

# Update to use blob endpoint instead of connection string
BLOB_ENDPOINT="https://$STORAGE_ACCOUNT.blob.core.windows.net"

az containerapp update \
  --name $CONTAINER_APP \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars \
    "BlobStorage__BlobEndpoint=$BLOB_ENDPOINT" \
    "BlobStorage__ContainerName=$CONTAINER_NAME" \
    "BlobStorage__BlobName=payment-validation-rules.json" \
  --remove-env-vars "BlobStorage__ConnectionString"
```

## Rules File Format

Location: `docs/templates/payment-validation-rules.json`

```json
[
  {
    "WorkflowName": "PaymentValidationRules",
    "Rules": [
      {
        "RuleName": "BR-001",
        "ErrorMessage": "Account balance is zero or negative",
        "ErrorType": "Error",
        "RuleExpressionType": "LambdaExpression",
        "Expression": "balance > 0"
      },
      {
        "RuleName": "BR-002",
        "ErrorMessage": "Account status 93 blocks posting",
        "ErrorType": "Error",
        "RuleExpressionType": "LambdaExpression",
        "Expression": "status != 93"
      },
      {
        "RuleName": "BR-003",
        "ErrorMessage": "Account is closed to posting",
        "ErrorType": "Error",
        "RuleExpressionType": "LambdaExpression",
        "Expression": "closedToPosting == 0"
      },
      {
        "RuleName": "BR-004",
        "ErrorMessage": "Payment amount exceeds account balance",
        "ErrorType": "Error",
        "RuleExpressionType": "LambdaExpression",
        "Expression": "amount <= balance"
      }
    ]
  }
]
```

## Updating Rules

### Option 1: Azure Portal

1. Navigate to Storage Account → Containers → `rules`
2. Click on `payment-validation-rules.json`
3. Click "Edit"
4. Make changes and save
5. Restart container app (or wait for cache refresh if implemented)

### Option 2: Azure CLI

```bash
# Download current rules
az storage blob download \
  --container-name $CONTAINER_NAME \
  --name payment-validation-rules.json \
  --file rules-backup.json \
  --connection-string "$CONNECTION_STRING"

# Edit the file locally
# vim rules-backup.json

# Upload updated rules
az storage blob upload \
  --container-name $CONTAINER_NAME \
  --name payment-validation-rules.json \
  --file rules-backup.json \
  --connection-string "$CONNECTION_STRING" \
  --overwrite
```

### Option 3: Azure Storage Explorer

1. Download and install [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)
2. Connect to your storage account
3. Navigate to the blob
4. Edit and upload

## Environment Variables Reference

| Variable | Description | Example |
|----------|-------------|---------|
| `BlobStorage__ConnectionString` | Connection string for storage account | `DefaultEndpointsProtocol=https;AccountName=...` |
| `BlobStorage__ContainerName` | Container name | `rules` |
| `BlobStorage__BlobName` | Blob file name | `payment-validation-rules.json` |

## Troubleshooting

### Rules not loading

Check container app logs:
```bash
az containerapp logs show \
  --name $CONTAINER_APP \
  --resource-group $RESOURCE_GROUP \
  --follow
```

Look for errors like:
- `Rules blob not found: rules/payment-validation-rules.json`
- `Error loading rules from blob storage`

### Connection issues

Verify connection string:
```bash
# Test connection
az storage container show \
  --name $CONTAINER_NAME \
  --connection-string "$CONNECTION_STRING"
```

### Invalid rules format

Validate JSON locally:
```bash
cat docs/templates/payment-validation-rules.json | jq .
```

## Future Enhancements

- **Hot Reload Endpoint**: Add `/api/ReloadRules` endpoint to refresh rules without restart
- **Rule Versioning**: Track rule versions in blob metadata
- **Caching**: Cache rules with TTL to reduce blob storage calls
- **Validation**: Add endpoint to validate rule syntax before uploading
