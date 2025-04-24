resource "azurerm_eventgrid_topic" "eventgrid_topic" {
  name                = local.eg_topic_name
  location            = local.location
  resource_group_name = azurerm_resource_group.messaging_rg.name
}

# New EventGrid subscription to the new Service Bus topic with eventgrid postfix
resource "azurerm_eventgrid_event_subscription" "to_servicebus_topic_eventgrid" {
  name                       = local.sub3_name
  scope                      = azurerm_eventgrid_topic.eventgrid_topic.id
  service_bus_topic_endpoint_id = azurerm_servicebus_topic.topic_eventgrid.id
  
  # Use dynamic delivery properties that will extract values from the subject  
  delivery_property {
    header_name = "SessionId"
    type        = "Dynamic"
    source_field = "data.properties.groupId"
    secret      = false
  }
  delivery_property {
    header_name = "MessageType"
    type        = "Dynamic"
    source_field = "data.properties.messageType"
    secret      = false
  }

   # Filter for high priority messages
  advanced_filter {
    string_in {
      key    = "data.properties.priority"
      values = ["high"]
    }
  }
}

# New EventGrid subscription to the new Event Hub with eventgrid postfix
resource "azurerm_eventgrid_event_subscription" "to_eventhub_eventgrid" {
  name                = local.sub4_name
  scope               = azurerm_eventgrid_topic.eventgrid_topic.id
  eventhub_endpoint_id = azurerm_eventhub.eventhub2_eventgrid.id
  
  # Use dynamic delivery properties that will extract values from the subject  
  delivery_property {
    header_name = "partitionKey"
    type        = "Dynamic"
    source_field = "data.properties.groupId"
    secret      = false
  }
  delivery_property {
    header_name = "messageType"
    type        = "Dynamic"
    source_field = "data.properties.messageType"
    secret      = false
  }

 
  # Filter for high priority messages
  advanced_filter {
    string_in {
      key    = "data.properties.priority"
      values = ["high"]
    }
  }
}

# Role assignment for EventGrid topic
resource "azurerm_role_assignment" "eventgrid_sender" {
  scope                = azurerm_eventgrid_topic.eventgrid_topic.id
  role_definition_name = "EventGrid Data Sender"
  principal_id         = "1a46de39-1478-497c-9f6b-ae2b586b4c9d"
}