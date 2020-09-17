### MAIN

resource "azurerm_resource_group" "onprem_tests" {
  name     = "aggregator-onprem-tests"
  location = var.resource_group_location

  tags = {
    source = "aggregator"
  }
}

# EOF

