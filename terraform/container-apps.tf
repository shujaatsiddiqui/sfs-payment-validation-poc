# Container App 1: Custom Connectors API
resource "azurerm_container_app" "custom_connectors" {
  name                         = var.custom_connectors_app_name
  resource_group_name          = azurerm_resource_group.payment.name
  container_app_environment_id = azurerm_container_app_environment.payment.id
  revision_mode                = "Single"

  template {
    min_replicas = var.custom_connectors_min_replicas
    max_replicas = var.custom_connectors_max_replicas

    container {
      name   = "custom-connectors"
      image  = "${azurerm_container_registry.payment.login_server}/custom-connectors:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "dev" ? "Development" : "Production"
      }

      env {
        name  = "RulesEngineApi__Url"
        value = "http://${var.rules_engine_app_name}"
      }
    }

    http_scale_rule {
      name                = "http-scale"
      concurrent_requests = 10
    }
  }

  registry {
    server   = azurerm_container_registry.payment.login_server
    username = azurerm_container_registry.payment.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.payment.admin_password
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

  tags = local.common_tags
}

# Container App 2: Rules Engine API
resource "azurerm_container_app" "rules_engine" {
  name                         = var.rules_engine_app_name
  resource_group_name          = azurerm_resource_group.payment.name
  container_app_environment_id = azurerm_container_app_environment.payment.id
  revision_mode                = "Single"

  template {
    min_replicas = var.rules_engine_min_replicas
    max_replicas = var.rules_engine_max_replicas

    container {
      name   = "rules-engine"
      image  = "${azurerm_container_registry.payment.login_server}/rules-engine-api:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment == "dev" ? "Development" : "Production"
      }
    }

    http_scale_rule {
      name                = "http-scale"
      concurrent_requests = 10
    }
  }

  registry {
    server   = azurerm_container_registry.payment.login_server
    username = azurerm_container_registry.payment.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.payment.admin_password
  }

  ingress {
    external_enabled = false # Internal only
    target_port      = 80
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = local.common_tags
}

# Container App 3: React UI
resource "azurerm_container_app" "react_ui" {
  name                         = var.react_ui_app_name
  resource_group_name          = azurerm_resource_group.payment.name
  container_app_environment_id = azurerm_container_app_environment.payment.id
  revision_mode                = "Single"

  template {
    min_replicas = var.react_ui_min_replicas
    max_replicas = var.react_ui_max_replicas

    container {
      name   = "react-ui"
      image  = "${azurerm_container_registry.payment.login_server}/react-app:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "REACT_APP_RULES_API_URL"
        value = "http://${var.rules_engine_app_name}"
      }

      env {
        name  = "REACT_APP_CUSTOM_CONNECTOR_URL"
        value = "https://${azurerm_container_app.custom_connectors.ingress[0].fqdn}"
      }
    }

    http_scale_rule {
      name                = "http-scale"
      concurrent_requests = 10
    }
  }

  registry {
    server   = azurerm_container_registry.payment.login_server
    username = azurerm_container_registry.payment.admin_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.payment.admin_password
  }

  ingress {
    external_enabled = true # Public UI
    target_port      = 80
    transport        = "http"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  tags = local.common_tags
}
