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