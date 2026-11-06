# SFS Payment Validation - Custom Connector Registration Guide

## Overview
This guide walks you through registering the SFS Payment Validation API as a Custom Connector in Azure, which will allow it to appear in Logic Apps with a branded UI.

## Prerequisites
✅ Function deployed and working: https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/SfsValidatePayment

✅ OpenAPI spec ready: `docs/sfs-connector-openapi.yaml`

✅ Azure AD App registered: Client ID `4b84ff16-70ef-4eb6-b55f-81845e4c4030`

---

## Step-by-Step Registration

### Step 1: Create Custom Connector

1. **Open Azure Portal**
   - Go to: https://portal.azure.com/#create/Microsoft.CustomConnector
   - Or search for "Custom Connectors" in Azure Portal

2. **Basics Tab**
   - **Subscription:** Azure subscription 1
   - **Resource Group:** rg-sfs-payment-validation (or create new)
   - **Region:** Central US
   - **Connector Name:** `SFS-Payment-Validation`
   - **Connector Display Name:** `SFS Payment Validation`

3. **Click:** "Review + create" → "Create"

---

### Step 2: Import OpenAPI Definition

1. **Navigate to your Custom Connector**
   - Portal → Custom Connectors → SFS-Payment-Validation

2. **Click:** "Edit" (pencil icon)

3. **General Tab**
   - **Import mode:** Select "OpenAPI file"
   - **Upload:** Browse to `/docs/sfs-connector-openapi.yaml`
   - Or manually enter:
     - **Scheme:** HTTPS
     - **Host:** `ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io`
     - **Base URL:** `/`

4. **Description** (optional):
   ```
   Custom connector for SFS Payment Validation.
   Validates payments against business rules BR-005 (Zero Amount) and BR-008 (Account Status 93).
   ```

5. **Click:** "Continue" or "Update connector"

---

### Step 3: Configure Security (OAuth 2.0)

**Option A: No Authentication (Quick Test)**

For quick POC testing:
1. **Security Tab**
2. **Authentication type:** No authentication
3. **Click:** "Update connector"
4. **Skip to Step 4**

**Option B: OAuth 2.0 with Azure AD (Production)**

For production-ready setup:

1. **Security Tab**
2. **Authentication type:** OAuth 2.0
3. **Identity Provider:** Azure Active Directory
4. **OAuth 2.0 settings:**
   - **Client ID:** `4b84ff16-70ef-4eb6-b55f-81845e4c4030`
   - **Client secret:** (Need to create in Azure AD - see below)
   - **Resource URL:** `api://4b84ff16-70ef-4eb6-b55f-81845e4c4030`
   - **Scope:** (leave empty or use: `user_impersonation`)

5. **Authorization URL:**
   ```
   https://login.microsoftonline.com/70a70b89-89ca-4957-a589-a917166872d1/oauth2/v2.0/authorize
   ```

6. **Token URL:**
   ```
   https://login.microsoftonline.com/70a70b89-89ca-4957-a589-a917166872d1/oauth2/v2.0/token
   ```

7. **Refresh URL:** (same as Token URL)

8. **Click:** "Update connector"

#### Creating Client Secret (if using OAuth):

```bash
# In Azure Portal or CLI:
az ad app credential reset --id 4b84ff16-70ef-4eb6-b55f-81845e4c4030
```

Save the returned secret securely!

---

### Step 4: Review Definition

1. **Definition Tab**
   - You should see: **SfsValidatePayment** operation
   - **Summary:** Validate SFS Payment
   - **Description:** Validates payment against business rules

2. **Request:**
   - **paymentId** (string, required)
   - **ruleSelection** (string, optional): "All Rules", "BR-005", or "BR-008"

3. **Response (200 OK):**
   - **isValid** (boolean)
   - **errors** (array of strings)

4. **Click:** "Update connector"

---

### Step 5: Test the Connector

1. **Test Tab**
2. **Click:** "New connection"
3. **Create Connection:**
   - If using "No auth": Just click "Create"
   - If using OAuth: Sign in with Azure AD
4. **Test the operation:**
   - **Operation:** SfsValidatePayment
   - **paymentId:** `PAY-001`
   - **ruleSelection:** `All Rules`
5. **Click:** "Test operation"

**Expected Response:**
```json
{
  "isValid": true,
  "errors": []
}
```

**Test Additional Scenarios:**
- **PAY-002** → Should return BR-005 error
- **PAY-003** → Should return BR-008 error
- **PAY-004** → Should return both errors

---

### Step 6: Use in Logic App

1. **Create or open a Logic App**
2. **Add a new step**
3. **Search for:** "SFS Payment Validation"
4. **You should see:** Custom connector with your operations!
5. **Select:** "Validate SFS Payment" action
6. **Configure:**
   - Connection: Select existing or create new
   - paymentId: Can use dynamic content or hardcode
   - ruleSelection: Choose from dropdown

---

## Troubleshooting

### Connector Not Appearing in Logic Apps

**Solution:**
- Make sure the connector is in the same region as your Logic App
- Check that you've created a connection successfully
- Try refreshing the Logic App designer

### Test Fails with Timeout

**Solution:**
- Verify the Container App is running:
  ```bash
  az containerapp show --name ca-sfs-payment-validation --resource-group rg-sfs-payment-validation --query "properties.runningStatus"
  ```
- Test the endpoint directly:
  ```bash
  curl https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/SfsValidatePayment -X POST -H "Content-Type: application/json" -d '{"paymentId":"PAY-001","ruleSelection":"All Rules"}'
  ```

### OAuth Authentication Fails

**Solution:**
- Verify Client ID and Secret are correct
- Check that the redirect URL in Azure AD app matches the connector's redirect URL
- Ensure the resource URL matches: `api://<client-id>`

---

## Quick Reference

### Endpoint URLs
- **Container App:** https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io
- **API Endpoint:** /api/SfsValidatePayment

### Azure AD Application
- **Client ID:** 4b84ff16-70ef-4eb6-b55f-81845e4c4030
- **Tenant ID:** 70a70b89-89ca-4957-a589-a917166872d1
- **Resource ID:** api://4b84ff16-70ef-4eb6-b55f-81845e4c4030

### Test Payment IDs
- **PAY-001:** Valid payment (passes all rules)
- **PAY-002:** Invalid (BR-005: Zero amount)
- **PAY-003:** Invalid (BR-008: Status 93)
- **PAY-004:** Invalid (Both rules violated)

---

## Next Steps

After successful connector registration:

1. ✅ Test connector in Logic App
2. ✅ Create a sample workflow
3. ✅ Document the integration pattern
4. ✅ Add more operations as needed (CheckDuplicate, EnrichAccount, etc.)

---

## Support

For issues or questions:
- Check Azure Container App logs:
  ```bash
  az containerapp logs show --name ca-sfs-payment-validation --resource-group rg-sfs-payment-validation --follow
  ```
- Review Application Insights for telemetry
- Test the API endpoint directly with curl/Postman
