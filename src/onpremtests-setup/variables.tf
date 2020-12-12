### VARIABLES

# variable "azurerm_subscription_id" {}
# variable "azurerm_client_id" {}
# variable "azurerm_client_secret" {}
# variable "azurerm_tenant_id" {}

variable "dev_vm_name" {
  default = "aggregator-dev"
}
variable "dev10_vm_name" {
  default = "aggregatordev10"
}
variable "dev_vm_admin_username" {}
variable "dev_vm_admin_password" {}

variable "resource_group_location" {
  default = "westeurope"
}

# EOF #

