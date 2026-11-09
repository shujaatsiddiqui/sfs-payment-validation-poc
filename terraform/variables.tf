# Azure Subscription
variable "subscription_id" {
  description = "Azure Subscription ID for deployment"
  type        = string
}

# Environment and Location
variable "environment" {
  description = "Environment name (dev, qa, uat, prd)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "qa", "uat", "prd"], var.environment)
    error_message = "Environment must be one of: dev, qa, uat, prd"
  }
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "East US"
}

variable "region_code" {
  description = "Region code for naming (eus = East US)"
  type        = string
  default     = "eus"
}

variable "business_group" {
  description = "Business functional group code"
  type        = string
  default     = "payment"
}

# Resource Group
variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "dev-eus-rg-payment-01"
}

# Container Registry
variable "container_registry_name" {
  description = "Name of the Azure Container Registry (must be globally unique, alphanumeric only)"
  type        = string
  default     = "deveusacrpayment01"

  validation {
    condition     = can(regex("^[a-zA-Z0-9]{5,50}$", var.container_registry_name))
    error_message = "Container registry name must be 5-50 characters, alphanumeric only."
  }
}

# Container Apps Environment
variable "container_app_env_name" {
  description = "Name of the Container Apps Environment"
  type        = string
  default     = "dev-eus-ace-payment-01"
}

# Log Analytics Workspace
variable "log_analytics_name" {
  description = "Name of Log Analytics Workspace"
  type        = string
  default     = "dev-eus-law-payment-01"
}

# Container Apps Configuration
variable "custom_connectors_app_name" {
  description = "Name of the Custom Connectors Container App"
  type        = string
  default     = "dev-eus-ca-payment-connectors-01"
}

variable "rules_engine_app_name" {
  description = "Name of the Rules Engine Container App"
  type        = string
  default     = "dev-eus-ca-payment-rules-01"
}

variable "react_ui_app_name" {
  description = "Name of the React UI Container App"
  type        = string
  default     = "dev-eus-ca-payment-ui-01"
}

# Container Apps Scaling Configuration
variable "custom_connectors_min_replicas" {
  description = "Minimum number of replicas for Custom Connectors API"
  type        = number
  default     = 0 # Scale to zero for cost savings
}

variable "custom_connectors_max_replicas" {
  description = "Maximum number of replicas for Custom Connectors API"
  type        = number
  default     = 10
}

variable "rules_engine_min_replicas" {
  description = "Minimum number of replicas for Rules Engine API"
  type        = number
  default     = 0 # Scale to zero for cost savings
}

variable "rules_engine_max_replicas" {
  description = "Maximum number of replicas for Rules Engine API"
  type        = number
  default     = 5
}

variable "react_ui_min_replicas" {
  description = "Minimum number of replicas for React UI"
  type        = number
  default     = 1 # Always at least 1 for user access
}

variable "react_ui_max_replicas" {
  description = "Maximum number of replicas for React UI"
  type        = number
  default     = 3
}

# Required Tags (per company standards)
variable "cost_center" {
  description = "Cost center for billing (required tag)"
  type        = string

  validation {
    condition     = length(var.cost_center) > 0
    error_message = "Cost center is required."
  }
}

variable "owner" {
  description = "Resource owner email address (required tag)"
  type        = string

  validation {
    condition     = can(regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$", var.owner))
    error_message = "Owner must be a valid email address."
  }
}

variable "application" {
  description = "Application name (required tag)"
  type        = string
  default     = "Payment Validation System"
}

# Additional Tags
variable "additional_tags" {
  description = "Additional tags to apply to resources"
  type        = map(string)
  default     = {}
}
