# TODO - SFS Payment Validation POC

## High Priority

### 🔐 Add OAuth Authentication with Managed Identity for ACA

**Status:** Pending
**Priority:** High
**Estimated Effort:** Medium

**Current State:**
- ACA endpoint is publicly accessible without authentication
- Custom connector has no security definitions in swagger
- API endpoints in SfsPaymentApi have no authentication middleware

**Implementation Steps:**

1. **Enable Managed Identity on ACA**
   - [ ] Enable system-assigned managed identity on `ca-sfs-payment-validation`
   - [ ] Note the Object ID for later use
   - [ ] Grant necessary permissions to managed identity

2. **Register Azure AD Application**
   - [ ] Create app registration for the API in Azure AD
   - [ ] Configure API permissions and scopes (e.g., `api://sfs-payment-api/Payment.Validate`)
   - [ ] Add the managed identity as an authorized client
   - [ ] Note Application (Client) ID and Tenant ID

3. **Update API Code (SfsPaymentApi)**
   - [ ] Add `Microsoft.Identity.Web` NuGet package to SfsPaymentApi.csproj
   - [ ] Configure JWT Bearer authentication in Program.cs
   - [ ] Add `[Authorize]` attributes to PaymentController
   - [ ] Validate Azure AD tokens and claims
   - [ ] Add authentication middleware to pipeline

4. **Update Custom Connector Swagger**
   - [ ] Add OAuth2 `securityDefinitions` section
   - [ ] Configure authorization URL: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize`
   - [ ] Configure token URL: `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`
   - [ ] Add security requirements to all operations
   - [ ] Define required scopes

5. **Update Logic App Connection**
   - [ ] Reconfigure custom connector connection with OAuth settings
   - [ ] Configure managed identity authentication
   - [ ] Test connection and authentication flow
   - [ ] Update workflow to use authenticated connector

6. **Rebuild and Deploy**
   - [ ] Build updated Docker image with authentication: `docker buildx build --platform linux/amd64 --no-cache -t acrsfspay1793941559.azurecr.io/sfs-payment-api:latest --push`
   - [ ] Deploy to ACA: `az containerapp update --name ca-sfs-payment-validation --resource-group rg-sfs-payment-validation --image ...`
   - [ ] Test authenticated endpoints with valid token
   - [ ] Verify unauthorized access is blocked (401)

7. **Update Documentation**
   - [ ] Document authentication configuration in README
   - [ ] Add security setup instructions
   - [ ] Document how to obtain tokens for testing
   - [ ] Update API documentation with auth requirements

**Files to Modify:**
- `functions/SfsPaymentApi/Program.cs`
- `functions/SfsPaymentApi/SfsPaymentApi.csproj`
- `functions/SfsPaymentApi/Controllers/PaymentController.cs`
- `docs/sfs-custom-connectors-swagger.json`
- `workflows/deploy-workflow.json` (add managed identity configuration)
- `README.md` (add security documentation)

**References:**
- [Azure Container Apps Managed Identity](https://learn.microsoft.com/en-us/azure/container-apps/managed-identity)
- [Custom Connector Azure AD Authentication](https://learn.microsoft.com/en-us/azure/logic-apps/custom-connector-azure-active-directory-authentication)
- [Microsoft.Identity.Web Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)

---

## Future Enhancements

### 📊 Add Monitoring and Logging
- [ ] Implement Application Insights for ACA
- [ ] Add structured logging to API endpoints
- [ ] Create alerts for authentication failures
- [ ] Monitor API performance metrics

### 🧪 Add Integration Tests
- [ ] Create integration tests for workflow
- [ ] Test authentication scenarios
- [ ] Add CI/CD pipeline for automated testing

### 📝 Documentation
- [ ] Create architecture diagram
- [ ] Document deployment process
- [ ] Add troubleshooting guide

---

## Completed ✓

- ✅ Refactor Azure Functions to ASP.NET Core API
- ✅ Deploy API to Azure Container Apps
- ✅ Create custom connector swagger definition
- ✅ Implement payment validation business rules (BR-001 to BR-008)
- ✅ Create Logic App workflow with custom connector
- ✅ Remove old SfsPaymentValidation Azure Function
- ✅ Clean up outdated workflow files

---

**Last Updated:** 2026-11-06
