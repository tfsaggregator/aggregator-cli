output "instrumentation_key" {
  value = azurerm_application_insights.production.instrumentation_key
}

output "app_id" {
  value = azurerm_application_insights.production.app_id
}

output "dev_instrumentation_key" {
  value = azurerm_application_insights.development.instrumentation_key
}

output "dev_app_id" {
  value = azurerm_application_insights.development.app_id
}
