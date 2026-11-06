# Azure Container Registry
resource "azurerm_container_registry" "acr" {
  name                = var.container_registry_name
  resource_group_name = azurerm_resource_group.sfs.name
  location            = azurerm_resource_group.sfs.location
  sku                 = "Basic"
  admin_enabled       = true

  tags = merge(var.tags, {
    Name = var.container_registry_name
  })
}

# Container Apps Environment
resource "azurerm_container_app_environment" "env" {
  name                       = var.container_app_env_name
  resource_group_name        = azurerm_resource_group.sfs.name
  location                   = azurerm_resource_group.sfs.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.sfs.id

  tags = merge(var.tags, {
    Name = var.container_app_env_name
  })
}

# Container App for Function
resource "azurerm_container_app" "function" {
  name                         = var.container_app_name
  resource_group_name          = azurerm_resource_group.sfs.name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  # Managed Identity
  identity {
    type = "SystemAssigned"
  }

  template {
    min_replicas = 0
    max_replicas = 10

    container {
      name   = "sfs-payment-validation"
      image  = "${azurerm_container_registry.acr.login_server}/sfs-payment-validation:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "APPLICATIONINSIGHTS_CONNECTION_STRING"
        value = azurerm_application_insights.sfs.connection_string
      }

      env {
        name  = "AzureWebJobsStorage"
        value = azurerm_storage_account.sfs.primary_connection_string
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name  = "AzureWebJobsScriptRoot"
        value = "/home/site/wwwroot"
      }

      env {
        name  = "AzureFunctionsJobHost__Logging__Console__IsEnabled"
        value = "true"
      }

      # Rules Engine Blob Storage Configuration
      env {
        name  = "BlobStorage__ConnectionString"
        value = azurerm_storage_account.rules.primary_connection_string
      }

      env {
        name  = "BlobStorage__ContainerName"
        value = var.rules_container_name
      }

      env {
        name  = "BlobStorage__BlobName"
        value = var.rules_blob_name
      }
    }

    http_scale_rule {
      name                = "http-scale"
      concurrent_requests = 10
    }
  }

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = merge(var.tags, {
    Name = var.container_app_name
  })
}
