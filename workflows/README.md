# Azure Logic App Workflows

This folder contains Azure Logic App Standard workflow definitions for payment validation processing.

## Active Workflow

### payment-validation-batch.json
**Purpose:** Batch payment validation with business rules engine integration

**Trigger:** Recurrence (every 3 minutes)

**Flow:**
```
1. Trigger: Recurrence (every 3 minutes)
   ↓
2. Get Payment Data (array of payments)
   ↓
3. Initialize counters (Processed, SentToLM, SentToSalesforce)
   ↓
4. ForEach payment in array:
   ├─ Validate Data (Rule #1: VIN != null, Credit Amount != 0)
   ├─ IF Rule #1 passes:
   │  ├─ Validate Account Posting (Rule #2: Credit Amount > 0)
   │  ├─ IF Rule #2 passes:
   │  │  └─ Post to LM Servicing ✓
   │  └─ ELSE:
   │     └─ Post to Salesforce Unposted Queue
   └─ ELSE (Rule #1 fails):
      └─ Post to Salesforce Unposted Queue
   ↓
5. Return summary:
   {
     "totalProcessed": 25,
     "sentToLM": 20,
     "sentToSalesforce": 5
   }
```

**API Endpoints Called:**
- `GET /api/GetPaymentData` - Retrieve batch of payments
- `POST /api/Validate` - Validate against Rule #1 (ValidatePaymentData)
- `POST /api/Validate` - Validate against Rule #2 (ValidateAccountPosting)
- `POST /api/PostToSalesforce` - Send to unposted queue
- `POST /api/PostToLMServicing` - Send to servicing system

**Custom Connector URL:**
`https://dev-eus-ca-payment-connectors-01.azurecontainerapps.io`

---

## Legacy Workflows (Archive)

### deploy-workflow.json
CI/CD deployment workflow (archived)

### get-pending-payments-workflow.json
Single payment retrieval workflow (archived)

### payment-validation-workflow.json
Original payment validation workflow without batch processing (archived)

---

## Deployment

These workflows are deployed as part of an Azure Logic App Standard app in Azure Container Apps.

**Resource Name:** `dev-eus-logicapp-payment-01` (if deployed separately)

OR integrated within the Custom Connector Container App.

---

## Testing

Test the workflow manually:
```bash
# Trigger the workflow via HTTP (if HTTP trigger is configured)
curl -X POST https://dev-eus-logicapp-payment-01.azurecontainerapps.io/api/workflows/payment-validation-batch/triggers/manual/paths/invoke \
  -H "Content-Type: application/json"
```

Or let it run on recurrence schedule (every 3 minutes).

---

## Monitoring

View workflow runs in:
- Azure Portal → Logic Apps → Runs History
- Application Insights (if configured)

---

## Configuration

Workflow uses these environment variables/settings:
- Custom Connector API URL: Hardcoded in workflow definition
- Recurrence interval: 3 minutes (configurable in trigger)

To update API URL, edit the workflow JSON and redeploy.
