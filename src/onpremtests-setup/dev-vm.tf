resource "azurerm_public_ip" "dev_vm" {
  name                    = "dev-publicip"
  resource_group_name     = azurerm_resource_group.onprem_tests.name
  location                = azurerm_resource_group.onprem_tests.location
  allocation_method       = "Static"
  idle_timeout_in_minutes = 30
  domain_name_label       = var.dev_vm_name

  tags = {
    source = "aggregator"
    platform = "server"
  }
}

resource "azurerm_public_ip" "dev10_vm" {
  name                    = "dev10-publicip"
  resource_group_name     = azurerm_resource_group.onprem_tests.name
  location                = azurerm_resource_group.onprem_tests.location
  allocation_method       = "Static"
  idle_timeout_in_minutes = 30
  domain_name_label       = var.dev10_vm_name

  tags = {
    source = "aggregator"
    platform = "client"
  }
}

# Request your IP 
data "http" "myip" {
  url = "https://api.ipify.org/"
}

resource "azurerm_network_security_group" "dev_vm" {
  name                = "dev-vm"
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location

  security_rule {
    name                       = "RDP"
    priority                   = 300
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "TCP"
    source_port_range          = "*"
    destination_port_range     = "3389"
    source_address_prefix      = chomp(data.http.myip.body)
    destination_address_prefix = "*"
  }

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_network_interface" "dev_vm" {
  name                = "dev-nic"
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location

  ip_configuration {
    name                          = "main"
    subnet_id                     = azurerm_subnet.dev_vms.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.dev_vm.id
  }

  tags = {
    source = "aggregator"
    platform = "server"
  }
}

resource "azurerm_network_interface" "dev10_vm" {
  name                = "dev10-nic"
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location

  ip_configuration {
    name                          = "main"
    subnet_id                     = azurerm_subnet.dev_vms.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.dev10_vm.id
  }

  tags = {
    source = "aggregator"
    platform = "client"
  }
}

resource "azurerm_network_interface_security_group_association" "dev_vm" {
  network_interface_id      = azurerm_network_interface.dev_vm.id
  network_security_group_id = azurerm_network_security_group.dev_vm.id
}

resource "azurerm_network_interface_security_group_association" "dev10_vm" {
  network_interface_id      = azurerm_network_interface.dev10_vm.id
  network_security_group_id = azurerm_network_security_group.dev_vm.id
}


resource "azurerm_windows_virtual_machine" "dev" {
  name                = var.dev_vm_name
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location
  size                = "Standard_D4s_v3"
  admin_username      = var.dev_vm_admin_username
  admin_password      = var.dev_vm_admin_password

  network_interface_ids = [
    azurerm_network_interface.dev_vm.id,
  ]

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Premium_LRS"
  }

  source_image_reference {
    publisher = "microsoftvisualstudio"
    offer     = "visualstudio2019latest"
    sku       = "vs-2019-comm-latest-ws2019"
    version   = "latest"
  }

  timezone = "UTC"

  tags = {
    source = "aggregator"
    platform = "server"
  }
}

resource "azurerm_windows_virtual_machine" "dev10" {
  name                = var.dev10_vm_name
  resource_group_name = azurerm_resource_group.onprem_tests.name
  location            = azurerm_resource_group.onprem_tests.location
  # nested virt required for WSL2 see https://docs.microsoft.com/en-us/azure/virtual-machines/acu
  size                = "Standard_D4s_v4"
  admin_username      = var.dev_vm_admin_username
  admin_password      = var.dev_vm_admin_password

  network_interface_ids = [
    azurerm_network_interface.dev10_vm.id,
  ]

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Premium_LRS"
  }

  source_image_reference {
    publisher = "MicrosoftWindowsDesktop"
    offer     = "Windows-10"
    sku       = "20h1-pro"
    version   = "latest"
  }

  timezone = "UTC"

  tags = {
    source = "aggregator"
    platform = "client"
  }
}

/*
resource "azurerm_dev_test_global_vm_shutdown_schedule" "dev" {
  virtual_machine_id = azurerm_virtual_machine.dev.id
  location           = azurerm_resource_group.onprem_tests.location
  enabled            = true

  daily_recurrence_time = "1900"
  timezone              = "UTC"

  notification_settings {
    enabled         = true
    time_in_minutes = "60"
    #webhook_url     = "https://sample-webhook-url.example.com"
    mail = "giuliovdev@hotmail.com"
  }

  tags = {
    source = "aggregator"
  }
}

resource "azurerm_virtual_machine_extension" "dev_vm" {
  name                       = "${azurerm_windows_virtual_machine.dev.name}-vmext"
  virtual_machine_id         = azurerm_windows_virtual_machine.dev.id
  publisher                  = "Microsoft.Powershell"
  type                       = "DSC"
  type_handler_version       = "2.80"
  auto_upgrade_minor_version = true

  settings = <<SETTINGS
    {
        "configuration": {
            "url"       : "https://aggregatoronpremtestswe.blob.core.windows.net/dsc/AzureDevOps.zip",
            "script"    : "AzureDevOps.ps1",
            "function"  : "AzureDevOps"
          }
    }
SETTINGS

  tags = {
    source = "aggregator"
  }
}
*/
