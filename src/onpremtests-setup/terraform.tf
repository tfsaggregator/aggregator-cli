terraform {
  required_version = "~> 0.12"
}

provider "http" {
  version = "~> 1.2"
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

# EOF

