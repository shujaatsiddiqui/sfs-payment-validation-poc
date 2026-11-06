# Storage Account for Rules Engine (separate from function storage)
resource "azurerm_storage_account" "rules" {
  name                     = var.rules_storage_account_name
  resource_group_name      = azurerm_resource_group.sfs.name
  location                 = azurerm_resource_group.sfs.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  # Enable blob versioning for rule changes tracking
  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 7
    }
  }

  tags = merge(var.tags, {
    Name    = var.rules_storage_account_name
    Purpose = "Rules Engine Configuration"
  })
}

# Blob Container for Rules
resource "azurerm_storage_container" "rules" {
  name                  = var.rules_container_name
  storage_account_name  = azurerm_storage_account.rules.name
  container_access_type = "private"
}

# Upload rules.json to blob storage
resource "azurerm_storage_blob" "payment_rules" {
  name                   = var.rules_blob_name
  storage_account_name   = azurerm_storage_account.rules.name
  storage_container_name = azurerm_storage_container.rules.name
  type                   = "Block"
  source                 = "${path.module}/../docs/templates/payment-validation-rules.json"
  content_type           = "application/json"

  depends_on = [azurerm_storage_container.rules]
}

# Role assignment for Container App Managed Identity to read blobs
# Note: Commented out for initial deployment - will add via Azure CLI after container app is updated
# resource "azurerm_role_assignment" "container_app_blob_reader" {
#   scope                = azurerm_storage_account.rules.id
#   role_definition_name = "Storage Blob Data Reader"
#   principal_id         = azurerm_container_app.function.identity[0].principal_id
#
#   depends_on = [
#     azurerm_container_app.function,
#     azurerm_storage_account.rules
#   ]
# }
