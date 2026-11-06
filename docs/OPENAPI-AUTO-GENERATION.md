# OpenAPI Auto-Generation - Implementation Guide

## ✅ What We Added

### **1. NuGet Package**
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.OpenApi" Version="1.5.1" />
```

### **2. OpenAPI Attributes on Function**
```csharp
[Function("SfsValidatePayment")]
[OpenApiOperation(
    operationId: "SfsValidatePayment",
    tags: new[] { "Payment Validation" },
    Summary = "Validate SFS Payment",
    Description = "Validates a payment against SFS business rules...",
    Visibility = OpenApiVisibilityType.Important
)]
[OpenApiRequestBody(
    contentType: "application/json",
    bodyType: typeof(ValidationRequest),
    Required = true
)]
[OpenApiResponseWithBody(
    statusCode: HttpStatusCode.OK,
    contentType: "application/json",
    bodyType: typeof(ValidationResponse)
)]
```

### **3. Model Classes**
- ValidationRequest
- ValidationResponse
- ErrorResponse
- Payment

---

## 🌐 Available OpenAPI Endpoints

Once deployed to Azure, your function will expose these endpoints:

### **OpenAPI/Swagger Endpoints:**

| Endpoint | Description |
|----------|-------------|
| `GET /api/swagger.json` | OpenAPI 2.0 (Swagger) specification in JSON format |
| `GET /api/swagger/ui` | Swagger UI for interactive API testing |
| `GET /api/openapi/v2.json` | OpenAPI 2.0 specification |
| `GET /api/openapi/v3.json` | OpenAPI 3.0 specification |

### **Business Function Endpoints:**

| Endpoint | Description |
|----------|-------------|
| `POST /api/SfsValidatePayment` | Payment validation endpoint (same as before) |

---

## 📋 Auto-Generated Swagger Specification

The OpenAPI package will generate a spec similar to this:

```json
{
  "swagger": "2.0",
  "info": {
    "title": "SFS Payment Validation API",
    "version": "1.0.0"
  },
  "host": "ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io",
  "schemes": ["https"],
  "paths": {
    "/api/SfsValidatePayment": {
      "post": {
        "tags": ["Payment Validation"],
        "summary": "Validate SFS Payment",
        "description": "Validates a payment against SFS business rules...",
        "operationId": "SfsValidatePayment",
        "consumes": ["application/json"],
        "produces": ["application/json"],
        "parameters": [{
          "name": "body",
          "in": "body",
          "required": true,
          "schema": {
            "$ref": "#/definitions/ValidationRequest"
          }
        }],
        "responses": {
          "200": {
            "description": "Validation completed successfully",
            "schema": {
              "$ref": "#/definitions/ValidationResponse"
            }
          }
        }
      }
    }
  },
  "definitions": {
    "ValidationRequest": {
      "type": "object",
      "required": ["paymentId"],
      "properties": {
        "paymentId": {
          "type": "string",
          "description": "The payment identifier to validate"
        },
        "ruleSelection": {
          "type": "string",
          "description": "Rule selection: All Rules, BR-005, BR-008",
          "default": "All Rules"
        }
      }
    },
    "ValidationResponse": {
      "type": "object",
      "properties": {
        "isValid": {
          "type": "boolean",
          "description": "Indicates whether the payment passed all validations"
        },
        "errors": {
          "type": "array",
          "items": {
            "type": "string"
          },
          "description": "List of validation errors"
        }
      }
    }
  }
}
```

---

## 🔄 How to Use Auto-Generated Spec with Custom Connector

### **Option A: Direct URL Import (After Deployment)**

1. **Deploy the updated function to Azure**
2. **In Custom Connector Portal:**
   - Go to: Edit → General tab
   - Import mode: **OpenAPI URL**
   - Enter: `https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/swagger.json`
   - Click "Import"
3. **Update connector**

### **Option B: Download and Upload**

1. **After deployment, download the spec:**
```bash
curl https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/swagger.json \
  -o auto-generated-swagger.json
```

2. **Add Custom Connector metadata:**
```json
{
  "...": "...",
  "definitions": {
    "ValidationRequest": {
      "properties": {
        "paymentId": {
          "type": "string",
          "x-ms-summary": "Payment ID",           // Add this
          "x-ms-visibility": "important"          // Add this
        }
      }
    }
  }
}
```

3. **Upload to Custom Connector Portal**

---

## 🎯 Benefits of Auto-Generation

| Benefit | Impact |
|---------|--------|
| **Always in Sync** | Spec updates automatically when code changes |
| **Less Manual Work** | No need to manually update YAML/JSON |
| **Type Safety** | Models are source of truth |
| **Documentation** | Swagger UI for testing |
| **Versioning** | Can generate v2 and v3 OpenAPI specs |

---

## 🚀 Next Steps

### **1. Rebuild and Redeploy**
```bash
# Rebuild Docker image with OpenAPI support
docker build -t sfs-payment-validation:openapi .

# Tag for ACR
docker tag sfs-payment-validation:openapi \
  <acr-name>.azurecr.io/sfs-payment-validation:latest

# Push to ACR
docker push <acr-name>.azurecr.io/sfs-payment-validation:latest

# Restart Container App
az containerapp revision restart \
  --name ca-sfs-payment-validation \
  --resource-group rg-sfs-payment-validation
```

### **2. Test OpenAPI Endpoints**
```bash
# Get swagger spec
curl https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/swagger.json

# Access Swagger UI in browser
https://ca-sfs-payment-validation.purpleglacier-ca7d6433.centralus.azurecontainerapps.io/api/swagger/ui
```

### **3. Update Custom Connector**
- Import the auto-generated spec
- Add x-ms-* metadata if needed
- Update connector

---

## 📝 Code Changes Summary

### **Files Modified:**
1. `SfsPaymentValidation.csproj` - Added OpenAPI package
2. `SfsValidatePayment.cs` - Added OpenAPI attributes
3. `Program.cs` - (Simplified, no changes needed)

### **Files Created:**
1. `Models/ErrorResponse.cs` - Error response model

### **Files to Rebuild:**
1. `Dockerfile` - No changes needed (works as-is)

---

## ⚠️ Important Notes

1. **OpenAPI attributes are metadata only** - They don't change function behavior
2. **The function still works exactly the same** - Business logic unchanged
3. **OpenAPI endpoints are read-only** - They just serve documentation
4. **Custom Connector metadata** - You may still need to add x-ms-summary and x-ms-visibility manually

---

## 🧪 Testing Locally (If .NET 8 is installed)

If you have .NET 8 SDK installed:

```bash
func start

# Then access:
# http://localhost:7071/api/swagger/ui
# http://localhost:7071/api/swagger.json
```

Currently, local machine has .NET 10, so local testing isn't possible without downgrading.

---

## ✅ Deployment Ready!

The code is ready to be built and deployed. Once deployed, the OpenAPI endpoints will be automatically available and you can import the auto-generated spec into your Custom Connector.
