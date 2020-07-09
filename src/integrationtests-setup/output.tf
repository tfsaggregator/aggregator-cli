data "azurerm_client_config" "current" {}
data "azuredevops_client_config" "current" {}

locals {
  defaultRuntimePath = "${path.module}/../aggregator-cli/FunctionRuntime.zip"
}

resource "local_file" "logon_data" {
  sensitive_content = templatefile("${path.module}/logon-data.tmpl",
    {
      SubscriptionID   = data.azurerm_client_config.current.subscription_id,
      TenantID         = data.azurerm_client_config.current.tenant_id,
      DisplayName      = azuread_service_principal.integration_tests.display_name,
      ObjectID         = azuread_service_principal.integration_tests.object_id,
      ApplicationID    = azuread_service_principal.integration_tests.application_id,
      ClientID         = azuread_service_principal.integration_tests.application_id, // alias
      Password         = random_password.service_principal_password.result,
      ResourceGroup    = azurerm_resource_group.integration_tests.name,
      Location         = azurerm_resource_group.integration_tests.location,
      DevOpsUrl        = data.azuredevops_client_config.current.organization_url,
      ProjectName      = azuredevops_project.integration_tests.project_name,
      PAT              = var.azdo_personal_access_token,
      RuntimeSourceUrl = "file://${abspath(local.defaultRuntimePath)}"
  })
  filename        = "${path.module}/../integrationtests-cli/logon-data.json"
  file_permission = "0640"
}
