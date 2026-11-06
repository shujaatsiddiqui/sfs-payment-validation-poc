# Azure Logic Apps - SFS Payment Validation Implementation Plan

## Executive Summary

This plan outlines the complete implementation of the SFS Payment Validation workflow using Azure Logic Apps Standard, migrating from the Elsa Workflows POC to a cloud-native Azure solution.

**Migration Path:** Elsa Workflows → Azure Logic Apps Standard + Azure Functions

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Technology Decisions](#technology-decisions)
3. [Prerequisites](#prerequisites)
4. [Project Structure](#project-structure)
5. [Phase 1: Local Development Setup](#phase-1-local-development-setup)
6. [Phase 2: Azure Function Development](#phase-2-azure-function-development)
7. [Phase 3: Logic App Workflow Creation](#phase-3-logic-app-workflow-creation)
8. [Phase 4: Local Testing](#phase-4-local-testing)
9. [Phase 5: Infrastructure Provisioning](#phase-5-infrastructure-provisioning)
10. [Phase 6: Deployment](#phase-6-deployment)
11. [Phase 7: Testing & Validation](#phase-7-testing--validation)
12. [Phase 8: Monitoring & Observability](#phase-8-monitoring--observability)
13. [Troubleshooting Guide](#troubleshooting-guide)
14. [Cost Estimation](#cost-estimation)

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│  External Client (Postman, API, Scheduler)         │
└─────────────────┬───────────────────────────────────┘
                  │ HTTPS POST
┌─────────────────▼───────────────────────────────────┐
│  Azure Logic App Standard                          │
│  Workflow: payment-validation-workflow             │
│  ┌──────────────────────────────────────────────┐  │
│  │ 1. Initialize payment array                  │  │
│  │ 2. ForEach payment loop                      │  │
│  │    ├─ Call SfsValidatePayment function      │  │
│  │    ├─ Check validation result                │  │
│  │    └─ Append to results array                │  │
│  │ 3. Return aggregated results                 │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────────────┘
                  │ HTTPS
┌─────────────────▼───────────────────────────────────┐
│  Azure Function App (.NET 8 Isolated)              │
│  Function: SfsValidatePayment                      │
│  ┌──────────────────────────────────────────────┐  │
│  │ Input: { paymentId, ruleSelection }         │  │
│  │                                              │  │
│  │ 1. Lookup payment in mock database          │  │
│  │ 2. Validate against business rules:         │  │
│  │    - BR-005: Zero Payment Amount             │  │
│  │    - BR-008: Account Status 93               │  │
│  │ 3. Return validation result                  │  │
│  │                                              │  │
│  │ Output: { isValid, errors[] }                │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│  Application Insights                              │
│  - Request/Response logging                        │
│  - Performance metrics                             │
│  - Error tracking                                  │
└─────────────────────────────────────────────────────┘
```

### Component Diagram

```
Azure Resource Group: rg-sfs-payment-validation
├── Storage Account: stsfspaymentval001
│   ├── Blob containers (workflow storage)
│   └── Queue storage (internal communication)
│
├── App Service Plan: plan-sfs-payment-validation
│   ├── SKU: WS1 (Workflow Standard)
│   └── OS: Windows
│
├── Function App: func-sfs-payment-validation
│   ├── Runtime: .NET 8 Isolated
│   ├── Functions:
│   │   └── SfsValidatePayment (HTTP Trigger)
│   └── App Settings:
│       ├── AzureWebJobsStorage
│       └── APPINSIGHTS_INSTRUMENTATIONKEY
│
├── Logic App: logic-sfs-payment-validation
│   ├── Workflows:
│   │   └── payment-validation-workflow (Stateful)
│   └── Connections:
│       └── Azure Functions binding
│
└── Application Insights: appi-sfs-payment-validation
    ├── Telemetry data
    └── Logs & metrics
```

---

## Technology Decisions

### 1. Logic Apps: Standard vs Consumption

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Logic App Tier** | **Standard** | • Local development support<br>• Single deployment unit<br>• Better VNet integration<br>• More cost-effective for POC |
| **Hosting** | App Service Plan | • Predictable costs<br>• Better for dev/test environments |
| **Workflow Type** | Stateful | • Need to track validation results<br>• Support for run history |

### 2. Validation Logic: RulesEngine vs Hardcoded

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Validation Approach** | **Hardcoded** | • Only 2 simple rules (BR-005, BR-008)<br>• Better performance<br>• Easier to debug<br>• No external dependencies<br>• Sufficient for POC |
| **Future Migration** | RulesEngine | • When rules exceed 10+<br>• When business users need to modify rules<br>• When rules change monthly |

### 3. Azure Function Configuration

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Runtime** | .NET 8 Isolated | • Latest stable version<br>• Better isolation model<br>• Improved performance |
| **Trigger Type** | HTTP | • Called from Logic App<br>• Synchronous validation needed |
| **Authorization** | Function Key | • Secure API calls<br>• Standard practice |

---

## Prerequisites

### Required Tools

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| **.NET SDK** | 8.0+ | Function development | [Download](https://dotnet.microsoft.com/download) |
| **Azure CLI** | 2.50+ | Azure operations | [Download](https://docs.microsoft.com/cli/azure/install-azure-cli) |
| **Terraform** | 1.5+ | Infrastructure as Code | [Download](https://www.terraform.io/downloads) |
| **Azure Functions Core Tools** | 4.x | Local function testing | `npm install -g azure-functions-core-tools@4` |
| **VS Code** | Latest | Code editing | [Download](https://code.visualstudio.com/) |
| **Git** | 2.0+ | Version control | [Download](https://git-scm.com/) |

### VS Code Extensions (Recommended)

```bash
# Install VS Code extensions
code --install-extension ms-azuretools.vscode-azurefunctions
code --install-extension ms-azuretools.vscode-azurelogicapps
code --install-extension hashicorp.terraform
code --install-extension ms-dotnettools.csharp
```

### Azure Prerequisites

- **Azure Subscription** (Free tier acceptable for POC)
- **Resource Provider Registration:**
  ```bash
  az provider register --namespace Microsoft.Web
  az provider register --namespace Microsoft.Logic
  az provider register --namespace Microsoft.Insights
  ```

- **Required Permissions:**
  - Contributor role on subscription or resource group
  - Ability to create App Service Plans, Functions, Logic Apps

### Local Development Environment

```bash
# Verify installations
dotnet --version          # Should be 8.0+
az --version             # Should be 2.50+
terraform --version      # Should be 1.5+
func --version           # Should be 4.x
git --version           # Should be 2.0+
```

---

## Project Structure

```
CustomConnectionLogicApps/
│
├── Activities/                          # Reference: Elsa custom activities
│   ├── CheckDuplicateActivity.cs        # Duplicate detection
│   ├── EnrichAccountActivity.cs         # Account enrichment
│   ├── ValidatePaymentActivity.cs       # Comprehensive validation
│   └── SfsValidatePaymentActivity.cs    # RulesEngine validation (reference)
│
├── Services/                            # Reference: Elsa services
│   ├── IPaymentValidationService.cs
│   └── SfsPaymentValidationService.cs
│
├── Rules/                               # Reference: Business rules
│   └── ValidationRules.json
│
├── Models/                              # Reference: Elsa models
│
├── functions/                           # ✅ Azure Functions
│   └── SfsPaymentValidation/
│       ├── SfsValidatePaymentFunction.cs    # Main function logic
│       ├── SfsPaymentValidation.csproj      # Project file
│       ├── Program.cs                       # Function host config
│       ├── host.json                        # Function app settings
│       ├── local.settings.json              # Local environment vars
│       └── .gitignore
│
├── workflows/                           # ✅ Logic App workflows
│   └── payment-validation-workflow.json
│
├── terraform/                           # ✅ Infrastructure as Code
│   ├── main.tf                          # Main infrastructure
│   ├── variables.tf                     # Input variables
│   └── outputs.tf                       # Output values
│
├── .gitignore                           # Git ignore rules
├── README.md                            # Project documentation
├── ELSA_README.md                       # Original Elsa docs
├── PLAN.md                              # This file
└── VDA_Peritus_RetailPaymentLogic_SS_Final.docx  # Business docs
```

---

## Phase 1: Local Development Setup

### Step 1.1: Clone and Navigate

```bash
# Navigate to project directory
cd /Users/shujaatsiddiqui/Downloads/Shujaat/POC/WorkflowEngine/CustomConnectionLogicApps

# Verify structure
ls -la
```

**Expected Output:**
```
Activities/
Services/
Rules/
Models/
functions/
workflows/
terraform/
README.md
PLAN.md
```

### Step 1.2: Initialize Git Repository

```bash
# Initialize git (if not already done)
git init

# Add files
git add .

# Initial commit
git commit -m "Initial commit: Azure Logic Apps SFS Payment Validation"

# Create .gitignore (already created)
cat .gitignore
```

### Step 1.3: Verify Function Project

```bash
cd functions/SfsPaymentValidation

# Restore NuGet packages
dotnet restore

# Build project
dotnet build

# Verify build success
echo $?  # Should output 0
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Phase 2: Azure Function Development

### Step 2.1: Understand the Function

**File:** `functions/SfsPaymentValidation/SfsValidatePaymentFunction.cs`

**Function Signature:**
```csharp
[Function("SfsValidatePayment")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
```

**Input Schema:**
```json
{
  "paymentId": "PAY-001",
  "ruleSelection": "All Rules"  // or "BR-005" or "BR-008"
}
```

**Output Schema:**
```json
{
  "isValid": true,
  "errors": []
}
```

### Step 2.2: Validation Logic

**Business Rules (Hardcoded):**

```csharp
// BR-005: Zero Payment Amount
if (payment.CreditAmount == 0)
{
    errors.Add("Zero Payment Amount");
}

// BR-008: Account status 93 blocks posting
if (payment.FIC_CLSFD_CD == 93)
{
    errors.Add("Account status 93 blocks posting");
}
```

**Mock Test Data:**

| PaymentId | CreditAmount | FIC_CLSFD_CD | Expected Result |
|-----------|--------------|--------------|-----------------|
| PAY-001   | 1000         | 10           | ✅ Valid (no errors) |
| PAY-002   | 0            | 10           | ❌ BR-005 violation |
| PAY-003   | 1000         | 93           | ❌ BR-008 violation |
| PAY-004   | 0            | 93           | ❌ BR-005 + BR-008 |

### Step 2.3: Local Function Testing

```bash
cd functions/SfsPaymentValidation

# Start function host
func start
```

**Expected Output:**
```
Azure Functions Core Tools
Core Tools Version:       4.x
Function Runtime Version: 4.x

Functions:
    SfsValidatePayment: [POST] http://localhost:7071/api/SfsValidatePayment
```

**Test with curl:**

```bash
# Test PAY-001 (Valid)
curl -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-001", "ruleSelection": "All Rules"}'

# Expected: {"isValid":true,"errors":[]}

# Test PAY-002 (BR-005 violation)
curl -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-002", "ruleSelection": "All Rules"}'

# Expected: {"isValid":false,"errors":["Zero Payment Amount"]}

# Test PAY-003 (BR-008 violation)
curl -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-003", "ruleSelection": "All Rules"}'

# Expected: {"isValid":false,"errors":["Account status 93 blocks posting"]}

# Test PAY-004 (Both violations)
curl -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-004", "ruleSelection": "All Rules"}'

# Expected: {"isValid":false,"errors":["Zero Payment Amount","Account status 93 blocks posting"]}
```

**Stop function host:** Press `Ctrl+C`

---

## Phase 3: Logic App Workflow Creation

### Step 3.1: Workflow Overview

**File:** `workflows/payment-validation-workflow.json`

**Workflow Steps:**

1. **Trigger:** HTTP Request (manual)
2. **Initialize Variables:**
   - `payments` (array) - Test payment data
   - `validationResults` (array) - Aggregated results
3. **ForEach Payment:**
   - Call `SfsValidatePayment` function
   - Check if valid
   - Append result to `validationResults`
4. **Response:** Return JSON with all results

### Step 3.2: Workflow Visual Representation

```
HTTP Request Trigger
  ↓
Initialize payment array
  ↓
Initialize results array
  ↓
ForEach payment
  ├─ SFS_Validate_Payment (Azure Function call)
  ├─ Condition: Check if valid
  │   ├─ TRUE → Append Valid Result
  │   └─ FALSE → Append Invalid Result
  └─ (loop next payment)
  ↓
HTTP Response (return results)
```

### Step 3.3: Key Workflow Sections

**ForEach Loop:**
```json
"ForEach_Payment": {
  "type": "Foreach",
  "foreach": "@variables('payments')",
  "actions": {
    "SFS_Validate_Payment": {
      "type": "Function",
      "inputs": {
        "body": {
          "paymentId": "@items('ForEach_Payment')['id']",
          "ruleSelection": "All Rules"
        }
      }
    }
  }
}
```

**Conditional Branching:**
```json
"Condition_Check_Valid": {
  "type": "If",
  "expression": {
    "equals": [
      "@body('SFS_Validate_Payment')['isValid']",
      true
    ]
  }
}
```

---

## Phase 4: Local Testing

### Step 4.1: Test Function Locally (Already Completed in Phase 2.3)

### Step 4.2: Prepare for Integration Testing

**Create test script:**

```bash
# Create test directory
mkdir -p test

# Create test script
cat > test/test-local.sh << 'EOF'
#!/bin/bash

echo "Starting local function..."
cd functions/SfsPaymentValidation
func start &
FUNC_PID=$!

sleep 5  # Wait for function to start

echo "Testing payment validation..."

echo "Test 1: PAY-001 (Valid)"
curl -s -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-001", "ruleSelection": "All Rules"}' | jq

echo "\nTest 2: PAY-002 (BR-005)"
curl -s -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-002", "ruleSelection": "All Rules"}' | jq

echo "\nTest 3: PAY-003 (BR-008)"
curl -s -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-003", "ruleSelection": "All Rules"}' | jq

echo "\nTest 4: PAY-004 (Both)"
curl -s -X POST http://localhost:7071/api/SfsValidatePayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-004", "ruleSelection": "All Rules"}' | jq

kill $FUNC_PID
echo "\nTests completed!"
EOF

chmod +x test/test-local.sh
```

**Run tests:**
```bash
./test/test-local.sh
```

---

## Phase 5: Infrastructure Provisioning

### Step 5.1: Azure Login

```bash
# Login to Azure
az login

# Set subscription (if you have multiple)
az account list --output table
az account set --subscription "<subscription-id>"

# Verify current subscription
az account show --output table
```

### Step 5.2: Review Terraform Configuration

**Navigate to terraform directory:**
```bash
cd terraform
```

**Review variables (customize if needed):**

```bash
# Edit variables.tf to customize names
nano variables.tf
```

**Key variables to review:**
- `resource_group_name` - Default: `rg-sfs-payment-validation`
- `location` - Default: `East US`
- `storage_account_name` - **Must be globally unique!** Default: `stsfspaymentval001`
- `function_app_name` - Default: `func-sfs-payment-validation`
- `logic_app_name` - Default: `logic-sfs-payment-validation`

**Create terraform.tfvars (optional):**
```bash
cat > terraform.tfvars << EOF
resource_group_name  = "rg-sfs-payment-poc"
location             = "East US"
storage_account_name = "stsfspoc$(date +%s)"  # Ensures uniqueness
function_app_name    = "func-sfs-payment-poc"
logic_app_name       = "logic-sfs-payment-poc"
EOF
```

### Step 5.3: Initialize Terraform

```bash
# Initialize Terraform
terraform init
```

**Expected Output:**
```
Initializing the backend...
Initializing provider plugins...
- Finding hashicorp/azurerm versions matching "~> 3.0"...
- Installing hashicorp/azurerm v3.x.x...

Terraform has been successfully initialized!
```

### Step 5.4: Plan Infrastructure

```bash
# Create execution plan
terraform plan -out=tfplan
```

**Review the plan output:**
- Should create ~7 resources:
  - Resource Group
  - Storage Account
  - App Service Plan
  - Function App
  - Logic App
  - Application Insights
  - Role Assignment

### Step 5.5: Apply Infrastructure

```bash
# Apply the plan
terraform apply tfplan
```

**This will take 5-10 minutes.**

**Expected Output:**
```
Apply complete! Resources: 7 added, 0 changed, 0 destroyed.

Outputs:

function_app_name = "func-sfs-payment-validation"
function_app_url = "https://func-sfs-payment-validation.azurewebsites.net"
logic_app_name = "logic-sfs-payment-validation"
logic_app_url = "https://logic-sfs-payment-validation.azurewebsites.net"
resource_group_name = "rg-sfs-payment-validation"
```

**Save the outputs:**
```bash
terraform output > ../deployment-info.txt
```

---

## Phase 6: Deployment

### Step 6.1: Deploy Function App

```bash
cd ../functions/SfsPaymentValidation

# Publish function to Azure
func azure functionapp publish func-sfs-payment-validation
```

**Expected Output:**
```
Getting site publishing info...
Creating archive for current directory...
Uploading 2.45 MB [###############################################]
Upload completed successfully.
Deployment completed successfully.
Syncing triggers...
Functions in func-sfs-payment-validation:
    SfsValidatePayment - [httpTrigger]
        Invoke url: https://func-sfs-payment-validation.azurewebsites.net/api/sfsvalidatepayment
```

**Get function key:**
```bash
FUNCTION_KEY=$(az functionapp keys list \
  --name func-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --query "functionKeys.default" -o tsv)

echo "Function Key: $FUNCTION_KEY"
```

**Test deployed function:**
```bash
curl -X POST "https://func-sfs-payment-validation.azurewebsites.net/api/SfsValidatePayment?code=$FUNCTION_KEY" \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "PAY-001", "ruleSelection": "All Rules"}'
```

### Step 6.2: Deploy Logic App Workflow

**Get Function App resource ID:**
```bash
FUNCTION_APP_ID=$(az functionapp show \
  --name func-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --query id -o tsv)

echo "Function App ID: $FUNCTION_APP_ID"
```

**Update workflow definition with Function App ID:**
```bash
cd ../../workflows

# Replace placeholder with actual Function App ID
sed "s|\\[concat(parameters('functionAppId'), '/functions/SfsValidatePayment')\\]|$FUNCTION_APP_ID/functions/SfsValidatePayment|g" \
  payment-validation-workflow.json > payment-validation-workflow-updated.json
```

**Deploy workflow using Azure CLI:**
```bash
# Note: Logic App Standard workflows are deployed via file system
# We'll use Azure Storage to upload the workflow

# Get Logic App storage connection string
STORAGE_CONN=$(az storage account show-connection-string \
  --name stsfspaymentval001 \
  --resource-group rg-sfs-payment-validation \
  --query connectionString -o tsv)

# Create workflow directory in storage
az storage file upload \
  --account-name stsfspaymentval001 \
  --share-name logic-sfs-payment-validation-content \
  --source payment-validation-workflow-updated.json \
  --path "site/wwwroot/payment-validation-workflow/workflow.json"
```

**Alternative: Deploy via VS Code**

1. Open VS Code
2. Install "Azure Logic Apps (Standard)" extension
3. Open `workflows` folder
4. Right-click workflow → "Deploy to Logic App"
5. Select your Logic App: `logic-sfs-payment-validation`

---

## Phase 7: Testing & Validation

### Step 7.1: Get Logic App Callback URL

```bash
# Get workflow trigger URL
az rest --method post \
  --uri "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-sfs-payment-validation/providers/Microsoft.Web/sites/logic-sfs-payment-validation/hostruntime/runtime/webhooks/workflow/api/management/workflows/payment-validation-workflow/triggers/manual/listCallbackUrl?api-version=2020-05-01" \
  --query value -o tsv
```

**Save the URL:**
```bash
WORKFLOW_URL="<paste-callback-url-here>"
```

### Step 7.2: Test End-to-End Workflow

**Test with sample payload:**
```bash
curl -X POST "$WORKFLOW_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "payments": [
      {"id": "PAY-001", "amount": 1000},
      {"id": "PAY-002", "amount": 0},
      {"id": "PAY-003", "amount": 1000},
      {"id": "PAY-004", "amount": 0}
    ]
  }'
```

**Expected Response:**
```json
{
  "message": "Payment validation completed",
  "results": [
    {
      "paymentId": "PAY-001",
      "status": "Valid",
      "message": "Payment is ready to post"
    },
    {
      "paymentId": "PAY-002",
      "status": "Invalid",
      "errors": ["Zero Payment Amount"]
    },
    {
      "paymentId": "PAY-003",
      "status": "Invalid",
      "errors": ["Account status 93 blocks posting"]
    },
    {
      "paymentId": "PAY-004",
      "status": "Invalid",
      "errors": ["Zero Payment Amount", "Account status 93 blocks posting"]
    }
  ]
}
```

### Step 7.3: Verify in Azure Portal

1. Navigate to Azure Portal: https://portal.azure.com
2. Go to Resource Group: `rg-sfs-payment-validation`
3. Open Logic App: `logic-sfs-payment-validation`
4. Click "Workflows" → `payment-validation-workflow`
5. View "Run History" to see execution details
6. Click on a run to see step-by-step execution

---

## Phase 8: Monitoring & Observability

### Step 8.1: Enable Application Insights

**Already configured via Terraform!**

**View logs:**
```bash
# Function App logs
az monitor app-insights metrics show \
  --app appi-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation \
  --metric requests/count
```

### Step 8.2: Query Logs

**Open Application Insights in Portal:**
1. Navigate to `appi-sfs-payment-validation`
2. Click "Logs"
3. Run queries:

```kusto
// Function invocations
traces
| where message contains "SfsValidatePayment"
| order by timestamp desc
| take 100

// Validation errors
traces
| where message contains "IsValid=false"
| project timestamp, message
| order by timestamp desc

// Performance metrics
requests
| where name contains "SfsValidatePayment"
| summarize avg(duration), count() by bin(timestamp, 1h)
```

### Step 8.3: Set Up Alerts

**Create alert for failed validations:**
```bash
az monitor metrics alert create \
  --name "High Failed Validations" \
  --resource-group rg-sfs-payment-validation \
  --scopes $(az functionapp show -n func-sfs-payment-validation -g rg-sfs-payment-validation --query id -o tsv) \
  --condition "count requests/failed > 10" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## Troubleshooting Guide

### Common Issues

#### 1. Function App Deployment Fails

**Error:** `Cannot find *.csproj file`

**Solution:**
```bash
# Ensure you're in the correct directory
cd functions/SfsPaymentValidation
ls *.csproj  # Should list SfsPaymentValidation.csproj
```

#### 2. Logic App Can't Call Function

**Error:** `Unauthorized` or `404 Not Found`

**Solution:**
```bash
# Verify function app is running
az functionapp show -n func-sfs-payment-validation -g rg-sfs-payment-validation --query state

# Check function key
az functionapp keys list -n func-sfs-payment-validation -g rg-sfs-payment-validation

# Verify role assignment
az role assignment list --assignee <logic-app-principal-id> --scope <function-app-id>
```

#### 3. Storage Account Name Already Taken

**Error:** `StorageAccountAlreadyTaken`

**Solution:**
```bash
# Generate unique name
UNIQUE_SUFFIX=$(date +%s)
az storage account create \
  --name "stsfspay$UNIQUE_SUFFIX" \
  --resource-group rg-sfs-payment-validation \
  --location eastus
```

#### 4. Terraform Apply Fails

**Error:** `Error creating Resource Group`

**Solution:**
```bash
# Check Azure login
az account show

# Re-login if needed
az login

# Retry with debug
terraform apply -auto-approve TF_LOG=DEBUG
```

---

## Cost Estimation

### Monthly Costs (Development Environment)

| Resource | SKU | Est. Cost |
|----------|-----|-----------|
| **App Service Plan** | WS1 (1 instance) | $75/month |
| **Function App** | Consumption (included in plan) | $0 |
| **Logic App** | Standard (included in plan) | $0 |
| **Storage Account** | Standard LRS (< 1GB) | $0.02/month |
| **Application Insights** | Pay-as-you-go (5GB free) | $0-5/month |
| **Bandwidth** | Minimal egress | $1/month |
| **TOTAL** | | **~$81/month** |

### Cost Optimization Tips

1. **Stop resources when not in use:**
   ```bash
   az functionapp stop -n func-sfs-payment-validation -g rg-sfs-payment-validation
   az logicapp stop -n logic-sfs-payment-validation -g rg-sfs-payment-validation
   ```

2. **Use consumption plan for production** (pay per execution)

3. **Delete entire resource group when done with POC:**
   ```bash
   terraform destroy
   ```

---

## Next Steps

### Phase 9: Enhancements (Future)

1. **Add More Custom Functions:**
   - CheckDuplicateFunction
   - EnrichAccountFunction
   - ValidatePaymentFunction (comprehensive)

2. **Integrate Real Data Sources:**
   - Azure SQL Database for payment data
   - Cosmos DB for account information
   - Service Bus for event-driven triggers

3. **Add CI/CD Pipeline:**
   - GitHub Actions workflow
   - Automated testing
   - Blue-green deployments

4. **Implement Security:**
   - Managed Identity for all connections
   - Key Vault for secrets
   - Private endpoints for networking

5. **Add Comprehensive Monitoring:**
   - Custom dashboards
   - Alerting rules
   - Performance tuning

---

## Appendix

### A. Useful Commands Cheat Sheet

```bash
# Azure CLI
az login
az account list
az account set --subscription <id>
az group list
az resource list -g <resource-group>

# Terraform
terraform init
terraform plan
terraform apply
terraform destroy
terraform output

# Azure Functions
func start
func azure functionapp publish <name>
az functionapp list
az functionapp logs tail -n <name> -g <rg>

# Logic Apps
az logicapp list
az logicapp show -n <name> -g <rg>
az logicapp workflow list -n <name> -g <rg>

# .NET
dotnet restore
dotnet build
dotnet publish
dotnet clean
```

### B. Resource Naming Conventions

| Resource Type | Naming Pattern | Example |
|---------------|----------------|---------|
| Resource Group | `rg-<project>-<env>` | `rg-sfs-payment-dev` |
| Storage Account | `st<project><random>` | `stsfspay20241105` |
| Function App | `func-<project>-<env>` | `func-sfs-payment-dev` |
| Logic App | `logic-<project>-<env>` | `logic-sfs-payment-dev` |
| App Insights | `appi-<project>-<env>` | `appi-sfs-payment-dev` |

### C. References

- [Azure Logic Apps Documentation](https://docs.microsoft.com/en-us/azure/logic-apps/)
- [Azure Functions .NET Isolated](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
- [Terraform Azure Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- [Elsa Workflows (Original)](../ELSA_README.md)

---

**Document Version:** 1.0
**Last Updated:** November 5, 2026
**Author:** Shujaat Siddiqui
**Co-Authored-By:** Claude Sonnet 4.5 <noreply@anthropic.com>
