resource "azurerm_eventhub_namespace" "eventhub_ns" {
  name                = local.eventhub_namespace
  location            = local.location
  resource_group_name = local.resource_group_name
  sku                 = "Standard"
}

resource "azurerm_eventhub" "eventhub1" {
  name                = local.eventhub1_name
  namespace_id        = azurerm_eventhub_namespace.eventhub_ns.id
  partition_count     = 2
  message_retention   = 1
}