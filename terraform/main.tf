# Data source for current Azure client configuration
data "azurerm_client_config" "current" {}

# Resource Group
resource "azurerm_resource_group" "sfs" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

# Storage Account for Function App
resource "azurerm_storage_account" "sfs" {
  name                     = var.storage_account_name
  resource_group_name      = azurerm_resource_group.sfs.name
  location                 = azurerm_resource_group.sfs.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  tags = merge(var.tags, {
    Name = var.storage_account_name
  })
}

# Application Insights
resource "azurerm_log_analytics_workspace" "sfs" {
  name                = "law-${var.app_insights_name}"
  resource_group_name = azurerm_resource_group.sfs.name
  location            = azurerm_resource_group.sfs.location
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = var.tags
}

resource "azurerm_application_insights" "sfs" {
  name                = var.app_insights_name
  resource_group_name = azurerm_resource_group.sfs.name
  location            = azurerm_resource_group.sfs.location
  workspace_id        = azurerm_log_analytics_workspace.sfs.id
  application_type    = "web"

  tags = merge(var.tags, {
    Name = var.app_insights_name
  })
}

# App Service Plan - DISABLED (using Container Apps instead)
# resource "azurerm_service_plan" "sfs" {
#   name                = var.app_service_plan_name
#   resource_group_name = azurerm_resource_group.sfs.name
#   location            = azurerm_resource_group.sfs.location
#   os_type             = "Linux"
#   sku_name            = var.app_service_plan_sku
#
#   tags = merge(var.tags, {
#     Name = var.app_service_plan_name
#   })
# }

# Function App with Managed Identity - DISABLED (using Container Apps instead)
# # resource "azurerm_linux_function_app" "sfs" {
#   name                = var.function_app_name
#   resource_group_name = azurerm_resource_group.sfs.name
#   location            = azurerm_resource_group.sfs.location
#   service_plan_id     = azurerm_service_plan.sfs.id
# 
#   storage_account_name       = azurerm_storage_account.sfs.name
#   storage_account_access_key = azurerm_storage_account.sfs.primary_access_key
# 
#   # Enable Managed Identity
#   identity {
#     type = "SystemAssigned"
#   }
# 
#   site_config {
#     application_stack {
#       dotnet_version              = var.dotnet_version
#       use_dotnet_isolated_runtime = true
#     }
# 
#     # CORS configuration for testing
#     cors {
#       allowed_origins = ["https://portal.azure.com"]
#     }
# 
#     # Enable Application Insights
#     application_insights_connection_string = azurerm_application_insights.sfs.connection_string
#     application_insights_key               = azurerm_application_insights.sfs.instrumentation_key
#   }
# 
#   app_settings = {
#     "FUNCTIONS_WORKER_RUNTIME"              = "dotnet-isolated"
#     "WEBSITE_RUN_FROM_PACKAGE"              = "1"
#     "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.sfs.connection_string
#     "AzureWebJobsStorage"                   = azurerm_storage_account.sfs.primary_connection_string
#     "FUNCTIONS_EXTENSION_VERSION"           = var.function_runtime_version
# 
#     # Enable managed identity for Azure AD auth
#     "WEBSITE_AUTH_ENABLED" = var.enable_azure_ad_auth ? "true" : "false"
#   }
# 
#   # Azure AD Authentication configuration
#   auth_settings_v2 {
#     auth_enabled           = var.enable_azure_ad_auth
#     require_authentication = false # Set to false initially, will configure after deployment
#     unauthenticated_action = "AllowAnonymous"
# 
#     login {
#       token_store_enabled = true
#     }
# 
#     active_directory_v2 {
#       client_id                  = azuread_application.function_app.client_id
#       tenant_auth_endpoint       = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}/v2.0"
#       allowed_audiences          = length(var.allowed_audiences) > 0 ? var.allowed_audiences : [
#         "api://${azuread_application.function_app.client_id}"
#       ]
#     }
#   }
# 
#   https_only = true
# 
#   tags = merge(var.tags, {
#     Name = var.function_app_name
#   })
# 
#   depends_on = [
#     azurerm_service_plan.sfs,
#     azurerm_storage_account.sfs,
#     azurerm_application_insights.sfs
#   ]
# }

# Azure AD Application for Function App
resource "azuread_application" "function_app" {
  display_name = "${var.function_app_name}-app"

  api {
    mapped_claims_enabled          = true
    requested_access_token_version = 2

    oauth2_permission_scope {
      admin_consent_description  = "Allow the application to access the SFS Payment Validation API"
      admin_consent_display_name = "Access SFS Payment Validation API"
      enabled                    = true
      id                         = "00000000-0000-0000-0000-000000000001"
      type                       = "User"
      user_consent_description   = "Allow the application to access the SFS Payment Validation API on your behalf"
      user_consent_display_name  = "Access SFS Payment Validation API"
      value                      = "user_impersonation"
    }
  }

  web {
    homepage_url = "https://${var.function_app_name}.azurewebsites.net"

    implicit_grant {
      access_token_issuance_enabled = true
      id_token_issuance_enabled     = true
    }
  }

  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    resource_access {
      id   = "e1fe6dd8-ba31-4d61-89e7-88639da4683d" # User.Read
      type = "Scope"
    }
  }
}

# Service Principal for the Azure AD App
resource "azuread_service_principal" "function_app" {
  client_id = azuread_application.function_app.client_id

  feature_tags {
    enterprise = true
  }
}
