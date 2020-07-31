### MAIN

resource "azurerm_resource_group" "production" {
  name     = "aggregator-telemetry"
  location = var.resource_group_location

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_application_insights" "production" {
  name                = "aggregator-telemetry"
  location            = azurerm_resource_group.production.location
  resource_group_name = azurerm_resource_group.production.name
  application_type    = "other"
  tags = {
    source = "aggregator"
  }
}

resource "azurerm_application_insights" "development" {
  name                = "aggregator-telemetry-dev"
  location            = azurerm_resource_group.production.location
  resource_group_name = azurerm_resource_group.production.name
  application_type    = "other"
  tags = {
    source = "aggregator"
  }
}

# EOF

