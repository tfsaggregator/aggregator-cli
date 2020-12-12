locals {
  vnet_address_space = "10.42.0.0/16"
}

resource "azurerm_virtual_network" "dev" {
  name                = "dev-network"
  address_space       = [local.vnet_address_space]
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_subnet" "dev_vms" {
  name                 = "internal"
  resource_group_name  = azurerm_resource_group.onprem_tests.name
  virtual_network_name = azurerm_virtual_network.dev.name
  address_prefix       = cidrsubnet(local.vnet_address_space, 8, 2)
}
