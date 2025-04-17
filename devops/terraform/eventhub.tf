resource "azurerm_eventhub_namespace" "eventhub_ns" {
  name                = local.eh_ns_name
  location            = local.location
  resource_group_name = azurerm_resource_group.messaging_rg.name
  sku                 = "Standard"
}

resource "azurerm_eventhub" "eventhub1" {
  name                = local.eh1_name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
  partition_count     = 2
  message_retention   = 1
}