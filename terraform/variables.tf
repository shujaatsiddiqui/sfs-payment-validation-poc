# Azure Subscription and Tenant
variable "subscription_id" {
  description = "Azure Subscription ID"
  type        = string
  default     = null # Will use Azure CLI default if not specified
}

variable "tenant_id" {
  description = "Azure AD Tenant ID"
  type        = string
  default     = null # Will use Azure CLI default if not specified
}

# Resource Group
variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-sfs-payment-validation"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "environment" {
  description = "Environment name (dev, test, prod)"
  type        = string
  default     = "dev"
}

# Storage Account
variable "storage_account_name" {
  description = "Name of the storage account (must be globally unique, 3-24 chars, lowercase alphanumeric)"
  type        = string
  default     = "stsfspaymentval"

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.storage_account_name))
    error_message = "Storage account name must be 3-24 characters, lowercase letters and numbers only."
  }
}

# App Service Plan
variable "app_service_plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "plan-sfs-payment-validation"
}

variable "app_service_plan_sku" {
  description = "SKU for the App Service Plan (Y1=Consumption, EP1=Elastic Premium, P1V2=Premium)"
  type        = string
  default     = "Y1" # Consumption plan for cost optimization
}

# Function App
variable "function_app_name" {
  description = "Name of the Function App"
  type        = string
  default     = "func-sfs-payment-validation"
}

variable "function_runtime_version" {
  description = "Azure Functions runtime version"
  type        = string
  default     = "~4"
}

variable "dotnet_version" {
  description = ".NET version for the Function App"
  type        = string
  default     = "10.0"
}

# Application Insights
variable "app_insights_name" {
  description = "Name of Application Insights"
  type        = string
  default     = "appi-sfs-payment-validation"
}

# Tags
variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default = {
    Project     = "SFS Payment Validation"
    ManagedBy   = "Terraform"
    Purpose     = "Custom Connector POC"
  }
}

# Azure AD Authentication
variable "enable_azure_ad_auth" {
  description = "Enable Azure AD authentication for the Function App"
  type        = bool
  default     = true
}

variable "allowed_audiences" {
  description = "Allowed audiences for Azure AD authentication"
  type        = list(string)
  default     = []
}

# Container Apps Configuration
variable "container_registry_name" {
  description = "Name of the Azure Container Registry (must be globally unique, alphanumeric only)"
  type        = string
  default     = "acrsfspaymentval"

  validation {
    condition     = can(regex("^[a-zA-Z0-9]{5,50}$", var.container_registry_name))
    error_message = "Container registry name must be 5-50 characters, alphanumeric only."
  }
}

variable "container_app_env_name" {
  description = "Name of the Container Apps Environment"
  type        = string
  default     = "cae-sfs-payment-validation"
}

variable "container_app_name" {
  description = "Name of the Container App"
  type        = string
  default     = "ca-sfs-payment-validation"
}

# Rules Engine Storage Account
variable "rules_storage_account_name" {
  description = "Name of the storage account for rules engine (must be globally unique, 3-24 chars, lowercase alphanumeric)"
  type        = string
  default     = "stsfspaymentrules"

  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.rules_storage_account_name))
    error_message = "Storage account name must be 3-24 characters, lowercase letters and numbers only."
  }
}

variable "rules_container_name" {
  description = "Name of the blob container for rules"
  type        = string
  default     = "rules"
}

variable "rules_blob_name" {
  description = "Name of the rules blob file"
  type        = string
  default     = "payment-validation-rules.json"
}

# Twilio SendGrid API Configuration
variable "twilio_api_key" {
  description = "SendGrid API Key for email sending (get from Twilio Console > Email API > Integration Guide)"
  type        = string
  default     = ""  # Set in terraform.tfvars (gitignored)
  sensitive   = true
}

variable "twilio_account_sid" {
  description = "Twilio Account SID (legacy - not used with SendGrid)"
  type        = string
  default     = ""  # Set in terraform.tfvars (gitignored)
  sensitive   = true
}

variable "twilio_auth_token" {
  description = "Twilio Auth Token (used as fallback if ApiKey not set)"
  type        = string
  default     = ""  # Set in terraform.tfvars (gitignored)
  sensitive   = true
}

variable "twilio_from_email" {
  description = "Default from email address for SendGrid emails"
  type        = string
  default     = ""  # Set in terraform.tfvars (gitignored)
}

variable "twilio_from_name" {
  description = "Default from name for SendGrid emails"
  type        = string
  default     = "SFS Payment Validation"
}
