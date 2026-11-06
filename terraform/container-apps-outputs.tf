# Container Registry Outputs
output "container_registry_name" {
  description = "Name of the Azure Container Registry"
  value       = azurerm_container_registry.acr.name
}

output "container_registry_login_server" {
  description = "Login server URL for the Container Registry"
  value       = azurerm_container_registry.acr.login_server
}

output "container_registry_admin_username" {
  description = "Admin username for the Container Registry"
  value       = azurerm_container_registry.acr.admin_username
  sensitive   = true
}

# Container App Environment Outputs
output "container_app_environment_name" {
  description = "Name of the Container Apps Environment"
  value       = azurerm_container_app_environment.env.name
}

output "container_app_environment_id" {
  description = "ID of the Container Apps Environment"
  value       = azurerm_container_app_environment.env.id
}

# Container App Outputs
output "container_app_name" {
  description = "Name of the Container App"
  value       = azurerm_container_app.function.name
}

output "container_app_url" {
  description = "FQDN of the Container App"
  value       = "https://${azurerm_container_app.function.ingress[0].fqdn}"
}

output "container_app_fqdn" {
  description = "Fully qualified domain name of the Container App"
  value       = azurerm_container_app.function.ingress[0].fqdn
}

output "container_app_principal_id" {
  description = "Principal ID of the Container App's Managed Identity"
  value       = azurerm_container_app.function.identity[0].principal_id
}

output "container_app_tenant_id" {
  description = "Tenant ID of the Container App's Managed Identity"
  value       = azurerm_container_app.function.identity[0].tenant_id
}

# Updated deployment summary for Container Apps
output "deployment_summary_aca" {
  description = "Summary of deployed Container Apps resources"
  value = {
    resource_group    = azurerm_resource_group.sfs.name
    location          = azurerm_resource_group.sfs.location
    container_app_url = "https://${azurerm_container_app.function.ingress[0].fqdn}"
    api_endpoint      = "https://${azurerm_container_app.function.ingress[0].fqdn}/api/SfsValidatePayment"
    managed_identity  = azurerm_container_app.function.identity[0].principal_id
    environment       = var.environment
    deployment_type   = "Azure Container Apps"
  }
}

# Updated custom connector config for Container Apps
output "custom_connector_config_aca" {
  description = "Configuration values for Custom Connector with Container Apps"
  value = {
    base_url              = "https://${azurerm_container_app.function.ingress[0].fqdn}"
    api_endpoint          = "/api/SfsValidatePayment"
    auth_type             = "oauth2"
    azure_ad_tenant_id    = data.azurerm_client_config.current.tenant_id
    azure_ad_client_id    = azuread_application.function_app.client_id
    resource_id           = "api://${azuread_application.function_app.client_id}"
    authorization_url     = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}/oauth2/v2.0/authorize"
    token_url             = "https://login.microsoftonline.com/${data.azurerm_client_config.current.tenant_id}/oauth2/v2.0/token"
  }
}
