resource "azurerm_eventgrid_topic" "eventgrid_topic" {
  name                = local.eventgrid_topic
  location            = local.location
  resource_group_name = local.resource_group_name

}

resource "azurerm_eventgrid_event_subscription" "to_servicebus_topic" {
  name                 = local.subscription1_name
  scope                = azurerm_eventgrid_topic.eventgrid_topic.id
  service_bus_topic_endpoint_id = azurerm_servicebus_topic.topic1.id
}

resource "azurerm_eventgrid_event_subscription" "to_eventhub" {
  name         = local.subscription2_name
  scope        = azurerm_eventgrid_topic.eventgrid_topic.id
  eventhub_endpoint_id = azurerm_eventhub.eventhub1.id
}