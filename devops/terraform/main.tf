provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "messaging_rg" {
  name     = local.resource_group_name
  location = local.location
}