terraform {
  required_version = "~> 0.12"
}

provider "random" {
  version = "~> 2.2"
}

provider "local" {
  version = "~> 1.4"
}

provider "azuread" {
  version = "~>0.7.0"
}

provider "azurerm" {
  version = "~>2.0.0"
  features {}
  // to logon use `az logon`
  # subscription_id = var.azurerm_subscription_id
  # client_id       = var.azurerm_client_id
  # client_secret   = var.azurerm_client_secret
  # tenant_id       = var.azurerm_tenant_id
}

provider "azuredevops" {
  version               = ">= 0.0.1"
  org_service_url       = "https://dev.azure.com/giuliovaad"
  personal_access_token = var.azdo_personal_access_token
}

# EOF

