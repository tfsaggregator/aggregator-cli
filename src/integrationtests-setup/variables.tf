### VARIABLES

# variable "azurerm_subscription_id" {}
# variable "azurerm_client_id" {}
# variable "azurerm_client_secret" {}
# variable "azurerm_tenant_id" {}

# this must be a variable as it will be saved in the generated json file
variable "azdo_personal_access_token" {}

variable "resource_group_location" {
  default = "westeurope"
}

# EOF #

