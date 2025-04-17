resource "azurerm_eventgrid_topic" "eventgrid_topic" {
  name                = local.eg_topic_name
  location            = local.location
  resource_group_name = azurerm_resource_group.messaging_rg.name

}

resource "azurerm_eventgrid_event_subscription" "to_servicebus_topic" {
  name                 = local.sub1_name
  scope                = azurerm_eventgrid_topic.eventgrid_topic.id
  service_bus_topic_endpoint_id = azurerm_servicebus_topic.topic1.id
}

resource "azurerm_eventgrid_event_subscription" "to_eventhub" {
  name         = local.sub2_name
  scope        = azurerm_eventgrid_topic.eventgrid_topic.id
  eventhub_endpoint_id = azurerm_eventhub.eventhub1.id
}

# Role assignment for EventGrid topic
resource "azurerm_role_assignment" "eventgrid_sender" {
  scope                = azurerm_eventgrid_topic.eventgrid_topic.id
  role_definition_name = "EventGrid Data Sender"
  principal_id         = "1a46de39-1478-497c-9f6b-ae2b586b4c9d"
}