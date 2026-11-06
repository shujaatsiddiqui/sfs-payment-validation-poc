# Output variables for rules storage account

output "rules_storage_account_name" {
  description = "Name of the rules storage account"
  value       = azurerm_storage_account.rules.name
}

output "rules_storage_account_id" {
  description = "ID of the rules storage account"
  value       = azurerm_storage_account.rules.id
}

output "rules_container_name" {
  description = "Name of the rules container"
  value       = azurerm_storage_container.rules.name
}

output "rules_blob_url" {
  description = "URL of the rules blob"
  value       = azurerm_storage_blob.payment_rules.url
}

output "rules_storage_connection_string" {
  description = "Connection string for rules storage account"
  value       = azurerm_storage_account.rules.primary_connection_string
  sensitive   = true
}

output "rules_blob_endpoint" {
  description = "Blob endpoint for rules storage account"
  value       = azurerm_storage_account.rules.primary_blob_endpoint
}
