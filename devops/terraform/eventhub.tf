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

resource "azurerm_eventhub_consumer_group" "backend_meetup_consumer" {
  name                = "backend-meetup-consumer"
  eventhub_name       = azurerm_eventhub.eventhub1.name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
}

# Role assignment for EventHub namespace
resource "azurerm_role_assignment" "eventhub_sender_receiver" {
  scope                = azurerm_eventhub_namespace.eventhub_ns.id
  role_definition_name = "Azure Event Hubs Data Owner"
  principal_id         = "1a46de39-1478-497c-9f6b-ae2b586b4c9d"
}