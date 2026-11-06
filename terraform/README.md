# SFS Payment Validation - Terraform Infrastructure

This directory contains Terraform configuration for deploying the SFS Payment Validation infrastructure to Azure.

## Architecture

```
Azure Resources:
├── Resource Group (rg-sfs-payment-validation)
├── Storage Accounts
│   ├── stsfspaymentval (Function storage - legacy)
│   └── stsfspaymentrules (Rules engine blob storage)
├── Container Registry (ACR)
├── Container Apps Environment
├── Container App (SFS Payment API)
├── Log Analytics Workspace
├── Application Insights
└── Azure AD Application (OAuth 2.0)
```

## Files

- **providers.tf** - Azure and Azure AD provider configuration
- **variables.tf** - Input variables with defaults
- **main.tf** - Resource group, storage, App Insights, Azure AD
- **container-apps.tf** - Container registry, environment, and app
- **storage-rules.tf** - Rules engine blob storage
- **container-apps-outputs.tf** - Container app outputs
- **storage-rules-outputs.tf** - Rules storage outputs
- **terraform.tfvars** - Variable values (customize this)

## Prerequisites

1. **Azure CLI** installed and authenticated:
   ```bash
   az login
   az account set --subscription "your-subscription-id"
   ```

2. **Terraform** installed (v1.0+):
   ```bash
   terraform version
   ```

3. **Docker** installed (for building images):
   ```bash
   docker --version
   ```

## Quick Start

### 1. Initialize Terraform

```bash
cd terraform
terraform init
```

### 2. Review and Customize Variables

Edit `terraform.tfvars` to customize your deployment:

```hcl
resource_group_name         = "rg-sfs-payment-validation"
location                    = "centralus"
environment                 = "dev"
storage_account_name        = "stsfspaymentval"
rules_storage_account_name  = "stsfspaymentrules"
container_registry_name     = "acrsfspay1793941559"  # Must be globally unique
container_app_env_name      = "cae-sfs-payment-validation"
container_app_name          = "ca-sfs-payment-validation"
```

**Important:** Storage account and container registry names must be globally unique across all of Azure!

### 3. Plan the Deployment

```bash
terraform plan
```

Review the resources that will be created:
- Resource group
- 2 storage accounts (function + rules)
- Container registry
- Container app environment
- Container app with managed identity
- Application Insights + Log Analytics
- Azure AD application
- Role assignments (Container App → Rules Storage)

### 4. Apply Configuration

```bash
terraform apply
```

Type `yes` when prompted. This will take approximately 5-10 minutes.

### 5. View Outputs

```bash
terraform output
```

Key outputs:
- `container_app_fqdn` - Container app URL
- `container_registry_login_server` - ACR login server
- `rules_blob_url` - URL of the uploaded rules file
- `rules_storage_connection_string` - Connection string (sensitive)

## Deployment Workflow

### Initial Setup

```bash
# 1. Apply Terraform
cd terraform
terraform init
terraform apply

# 2. Build and push Docker image
cd ../functions/SfsPaymentApi

# Get ACR name from Terraform output
ACR_NAME=$(cd ../../terraform && terraform output -raw container_registry_name)

# Login to ACR
az acr login --name $ACR_NAME

# Build for linux/amd64 and push
docker buildx build --platform linux/amd64 \
  -t $ACR_NAME.azurecr.io/sfs-payment-validation:latest \
  --push .

# 3. Update container app with new image
az containerapp update \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --image $ACR_NAME.azurecr.io/sfs-payment-validation:latest

# 4. Test the API
CONTAINER_URL=$(cd ../../terraform && terraform output -raw container_app_fqdn)
curl https://$CONTAINER_URL/api/GetPendingPayments
```

### Updating Code

```bash
# 1. Build and push new image
cd functions/SfsPaymentApi
ACR_NAME=acrsfspay1793941559  # Or get from terraform output

docker buildx build --platform linux/amd64 \
  -t $ACR_NAME.azurecr.io/sfs-payment-validation:latest \
  --push .

# 2. Trigger new revision
az containerapp update \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --image $ACR_NAME.azurecr.io/sfs-payment-validation:latest
```

### Updating Rules

Rules are automatically uploaded from `docs/templates/payment-validation-rules.json` during `terraform apply`.

**Option 1: Update via Terraform**
```bash
# Edit docs/templates/payment-validation-rules.json
# Then apply changes
terraform apply -target=azurerm_storage_blob.payment_rules
```

**Option 2: Update via Azure CLI**
```bash
STORAGE_NAME=$(terraform output -raw rules_storage_account_name)

az storage blob upload \
  --account-name $STORAGE_NAME \
  --container-name rules \
  --name payment-validation-rules.json \
  --file ../docs/templates/payment-validation-rules.json \
  --overwrite \
  --auth-mode login
```

**Option 3: Update via Azure Portal**
1. Navigate to Storage Account → Containers → `rules`
2. Click `payment-validation-rules.json`
3. Click "Edit" and make changes
4. Save

After updating rules, restart the container app:
```bash
az containerapp revision restart \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation
```

## Features

### 1. Rules Engine Blob Storage

- **Separate storage account** for rules (stsfspaymentrules)
- **Blob versioning** enabled for tracking changes
- **7-day retention** for deleted rules
- **Automatic upload** from `docs/templates/payment-validation-rules.json`
- **Role assignment** for container app managed identity

### 2. Container App with Managed Identity

- **System-assigned managed identity** enabled
- **Storage Blob Data Reader** role on rules storage
- **Scale-to-zero** enabled (min_replicas = 0)
- **Auto-scaling** up to 10 replicas

### 3. Environment Variables

The container app is automatically configured with:
```
APPLICATIONINSIGHTS_CONNECTION_STRING
AzureWebJobsStorage
FUNCTIONS_WORKER_RUNTIME
BlobStorage__ConnectionString
BlobStorage__ContainerName
BlobStorage__BlobName
```

### 4. Application Insights

- Monitoring and logging
- 30-day retention
- Connected to Log Analytics workspace

## Outputs

Retrieve output values:

```bash
# Container app URL
terraform output container_app_fqdn

# Container registry
terraform output container_registry_login_server

# Rules blob URL
terraform output rules_blob_url

# Connection string (sensitive)
terraform output -raw rules_storage_connection_string

# All outputs
terraform output
```

## Cost Optimization

- **Container App**: Scale-to-zero enabled (min_replicas = 0)
- **Storage**: LRS replication (cheapest)
- **Container Registry**: Basic tier
- **Application Insights**: 30-day retention

**Estimated monthly cost**: ~$5-10 (mostly Application Insights)

## Managed Identity (Production)

To use managed identity instead of connection string:

1. Update RulesEngineService.cs to support managed identity authentication
2. Remove connection string and use blob endpoint:

```bash
az containerapp update \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --remove-env-vars "BlobStorage__ConnectionString" \
  --set-env-vars \
    "BlobStorage__BlobEndpoint=$(terraform output -raw rules_blob_endpoint)" \
    "BlobStorage__ContainerName=rules" \
    "BlobStorage__BlobName=payment-validation-rules.json"
```

The managed identity already has **Storage Blob Data Reader** role assigned by Terraform.

## Troubleshooting

### Storage account name already exists
Storage account names must be globally unique. Update `rules_storage_account_name` in `terraform.tfvars`.

### Container registry name already exists
Container registry names must be globally unique. Update `container_registry_name` in `terraform.tfvars`.

### Container app fails to start
Check logs:
```bash
az containerapp logs show \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --follow
```

### Rules not loading
Verify blob exists:
```bash
STORAGE_NAME=$(terraform output -raw rules_storage_account_name)

az storage blob list \
  --account-name $STORAGE_NAME \
  --container-name rules \
  --auth-mode login \
  --output table
```

### Docker build fails
Ensure you're building for the correct platform:
```bash
docker buildx build --platform linux/amd64 ...
```

## Cleanup

To destroy all resources:

```bash
terraform destroy
```

**Warning:** This will delete all resources including storage, logs, container images, and Container App.

## Next Steps

1. ✅ Infrastructure deployed
2. 🔲 Build and push Docker image
3. 🔲 Test API endpoints
4. 🔲 Update rules in blob storage
5. 🔲 Configure custom domain (optional)
6. 🔲 Set up CI/CD pipeline
7. 🔲 Configure monitoring alerts
8. 🔲 Implement rule hot reload endpoint

## Resources

- [Azure Container Apps Terraform Docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_app)
- [Azure Storage Blob Terraform Docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/storage_blob)
- [Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [Microsoft Rules Engine](https://microsoft.github.io/RulesEngine/)
