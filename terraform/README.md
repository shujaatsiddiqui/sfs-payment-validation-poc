# SFS Payment Validation - Terraform Infrastructure

This directory contains Terraform configuration for deploying the Azure infrastructure required for the SFS Payment Validation custom connector.

## Architecture

```
Azure Resources Created:
├── Resource Group (rg-sfs-payment-validation)
├── Storage Account (stsfspaymentval)
├── Log Analytics Workspace
├── Application Insights (appi-sfs-payment-validation)
├── App Service Plan (Consumption Linux)
├── Function App (func-sfs-payment-validation)
│   ├── .NET 10 Isolated Runtime
│   ├── System-Assigned Managed Identity
│   └── Azure AD Authentication
└── Azure AD Application (for OAuth 2.0)
```

## Files

- **providers.tf** - Azure and Azure AD provider configuration
- **variables.tf** - Input variables with defaults
- **main.tf** - Main infrastructure resources
- **outputs.tf** - Output values after deployment
- **.gitignore** - Terraform-specific gitignore

## Prerequisites

1. **Azure CLI** installed and authenticated
   ```bash
   az login
   az account show
   ```

2. **Terraform** installed (v1.5.0+)
   ```bash
   terraform --version
   ```

3. **Azure Subscription** with appropriate permissions

## Quick Start

### 1. Initialize Terraform

```bash
cd terraform
terraform init
```

### 2. Review and Customize Variables

Create a `terraform.tfvars` file (optional):

```hcl
resource_group_name  = "rg-sfs-payment-poc"
location             = "East US"
storage_account_name = "stsfspoc20261105"  # Must be globally unique!
function_app_name    = "func-sfs-payment-poc"
environment          = "dev"
```

**Important:** `storage_account_name` must be globally unique across all of Azure!

### 3. Plan the Deployment

```bash
terraform plan -out=tfplan
```

Review the resources that will be created:
- ✅ Resource Group
- ✅ Storage Account
- ✅ Log Analytics Workspace
- ✅ Application Insights
- ✅ App Service Plan
- ✅ Function App (with Managed Identity)
- ✅ Azure AD Application
- ✅ Service Principal

### 4. Apply the Configuration

```bash
terraform apply tfplan
```

This will take approximately 5-10 minutes.

### 5. View Outputs

```bash
terraform output
```

**Key outputs:**
- `function_app_url` - Function App endpoint
- `function_app_principal_id` - Managed Identity ID
- `azure_ad_app_client_id` - Client ID for OAuth
- `custom_connector_config` - All values needed for Custom Connector

## Important Variables

### Required (if not using defaults)

| Variable | Default | Description |
|----------|---------|-------------|
| `subscription_id` | (auto-detected) | Azure Subscription ID |
| `tenant_id` | (auto-detected) | Azure AD Tenant ID |

### Optional (customizable)

| Variable | Default | Description |
|----------|---------|-------------|
| `resource_group_name` | rg-sfs-payment-validation | Resource group name |
| `location` | East US | Azure region |
| `storage_account_name` | stsfspaymentval | Storage account (must be unique!) |
| `function_app_name` | func-sfs-payment-validation | Function app name |
| `app_service_plan_sku` | Y1 (Consumption) | App Service Plan SKU |
| `dotnet_version` | 10.0 | .NET runtime version |
| `environment` | dev | Environment tag |
| `enable_azure_ad_auth` | true | Enable Azure AD authentication |

## Features

### 1. Managed Identity

The Function App is deployed with a **System-Assigned Managed Identity**:

```hcl
identity {
  type = "SystemAssigned"
}
```

**Benefits:**
- No need to manage credentials
- Secure access to Azure resources
- Used for Custom Connector authentication

### 2. Azure AD Authentication

Azure AD OAuth 2.0 authentication is configured:

```hcl
auth_settings_v2 {
  auth_enabled = true
  active_directory_v2 {
    client_id = azuread_application.function_app.client_id
    tenant_auth_endpoint = "https://login.microsoftonline.com/{tenant}/v2.0"
  }
}
```

**Configuration:**
- OAuth 2.0 with Azure AD
- Token store enabled
- Initially set to `AllowAnonymous` for testing
- Can be changed to `RedirectToLoginPage` for production

### 3. Application Insights

Monitoring is configured automatically:
- Connection string injected into Function App
- Log Analytics workspace for long-term storage
- 30-day retention period

### 4. .NET 10 Runtime

Function App configured for .NET 10 isolated worker:

```hcl
application_stack {
  dotnet_version              = "10.0"
  use_dotnet_isolated_runtime = true
}
```

## Outputs

After deployment, Terraform provides these outputs:

### Resource Information
```bash
terraform output resource_group_name
terraform output function_app_url
terraform output function_app_principal_id
```

### Custom Connector Configuration
```bash
terraform output custom_connector_config
```

This output provides all values needed to register the Custom Connector:
- Base URL
- API endpoint
- OAuth URLs
- Client ID
- Tenant ID

## Post-Deployment Steps

### 1. Deploy Function Code

```bash
cd ../functions/SfsPaymentValidation
func azure functionapp publish $(terraform -chdir=../../terraform output -raw function_app_name)
```

### 2. Test the Deployed Function

```bash
FUNCTION_URL=$(terraform output -raw function_app_url)
curl -X POST "$FUNCTION_URL/api/SfsValidatePayment" \
  -H "Content-Type: application/json" \
  -d '{"paymentId":"PAY-001","ruleSelection":"All Rules"}'
```

### 3. Register Custom Connector

Use the values from `custom_connector_config` output to register the Custom Connector in Azure Portal.

## Cost Estimation

### Consumption Plan (Default - Y1)

| Resource | Est. Monthly Cost |
|----------|-------------------|
| App Service Plan (Consumption) | $0 (1M executions free) |
| Storage Account | ~$0.02 |
| Application Insights | $0 (5GB free) |
| **Total** | **~$0-5/month** |

### Premium Plan (EP1)

Change `app_service_plan_sku = "EP1"` in variables:

| Resource | Est. Monthly Cost |
|----------|-------------------|
| App Service Plan (EP1) | ~$150/month |
| Storage Account | ~$0.02 |
| Application Insights | ~$5 |
| **Total** | **~$155/month** |

## Cleanup

To destroy all resources:

```bash
terraform destroy
```

**Warning:** This will delete all resources including storage, logs, and Function App.

## Troubleshooting

### Storage Account Name Already Taken

**Error:** `StorageAccountAlreadyTaken`

**Solution:** The storage account name must be globally unique. Change it in `terraform.tfvars`:

```hcl
storage_account_name = "stsfspoc$(date +%s)"
```

### Authentication Errors

**Error:** `Error authenticating to Azure`

**Solution:** Re-authenticate with Azure CLI:

```bash
az login
az account set --subscription "<subscription-id>"
```

### Terraform State Issues

**Error:** State file conflicts

**Solution:**
```bash
terraform init -reconfigure
terraform refresh
```

## Next Steps

1. ✅ Infrastructure deployed
2. 🔲 Deploy Function App code (Task #7)
3. 🔲 Register Custom Connector (Task #8)
4. 🔲 Create Logic App workflow (Task #9)
5. 🔲 Test end-to-end (Task #10)

## Resources

- [Azure Function App Terraform Docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/linux_function_app)
- [Azure AD Application Terraform Docs](https://registry.terraform.io/providers/hashicorp/azuread/latest/docs/resources/application)
- [Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
