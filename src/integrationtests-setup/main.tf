### MAIN

resource "azuread_application" "integration_tests" {
  name = "AggregatorIntegrationTests"
}

resource "azuread_service_principal" "integration_tests" {
  application_id               = azuread_application.integration_tests.application_id
  app_role_assignment_required = false
}

// we need to output this, so cannot 
resource "random_password" "service_principal_password" {
  keepers = {
    # Generate a new id each time we switch to a new AAD application
    app_id = azuread_application.integration_tests.application_id
  }

  length           = 24
  special          = true
  override_special = ".!-_=+"
}

locals {
  hours_in_a_year   = 24 * 365.25
  one_year_interval = format("%dh", local.hours_in_a_year)
  a_year_from_now   = timeadd(timestamp(), local.one_year_interval)
}

resource "azuread_service_principal_password" "integration_tests" {
  service_principal_id = azuread_service_principal.integration_tests.id
  value                = random_password.service_principal_password.result
  end_date             = "2099-01-01T01:02:03Z" //local.a_year_from_now
}

resource "azurerm_resource_group" "integration_tests" {
  name     = "aggregator-integration-tests"
  location = var.resource_group_location

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_resource_group" "integration_tests_scenario4" {
  name     = "aggregator-test-gv1"
  location = var.resource_group_location

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_role_assignment" "integration_tests" {
  scope                = azurerm_resource_group.integration_tests.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.integration_tests.id
}

resource "azurerm_role_assignment" "integration_tests_scenario4" {
  scope                = azurerm_resource_group.integration_tests_scenario4.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.integration_tests.id
}

resource "azuredevops_project" "integration_tests" {
  project_name = "AggregatorIntegrationTests"
  description  = "Integration test for Aggregator CLI"
}

# EOF

