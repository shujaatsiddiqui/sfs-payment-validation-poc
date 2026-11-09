# Local variables for common tags
locals {
  common_tags = merge(
    {
      "Cost Center" = var.cost_center
      "Owner"       = var.owner
      "Application" = var.application
      "Environment" = var.environment
      "ManagedBy"   = "Terraform"
      "Region"      = var.region_code
      "BizGroup"    = var.business_group
    },
    var.additional_tags
  )
}

# Resource Group
resource "azurerm_resource_group" "payment" {
  name     = var.resource_group_name
  location = var.location
  tags     = local.common_tags
}

# Container Registry
resource "azurerm_container_registry" "payment" {
  name                = var.container_registry_name
  resource_group_name = azurerm_resource_group.payment.name
  location            = azurerm_resource_group.payment.location
  sku                 = "Basic"
  admin_enabled       = true # Simplified authentication for POC

  tags = local.common_tags
}

# Log Analytics Workspace (required by Container App Environment)
resource "azurerm_log_analytics_workspace" "payment" {
  name                = var.log_analytics_name
  resource_group_name = azurerm_resource_group.payment.name
  location            = azurerm_resource_group.payment.location
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = local.common_tags
}

# Container Apps Environment
resource "azurerm_container_app_environment" "payment" {
  name                       = var.container_app_env_name
  resource_group_name        = azurerm_resource_group.payment.name
  location                   = azurerm_resource_group.payment.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.payment.id

  tags = local.common_tags
}
