resource "azurerm_servicebus_namespace" "servicebus1" {
  name                = local.sb1_name
  location            = local.location
  resource_group_name = azurerm_resource_group.messaging_rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_namespace" "servicebus2" {
  name                = local.sb2_name
  location            = local.location
  resource_group_name = azurerm_resource_group.messaging_rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_topic" "topic1" {
  name                 = local.topic1_name
  namespace_id         = azurerm_servicebus_namespace.servicebus1.id
  partitioning_enabled = true
}

resource "azurerm_servicebus_topic" "topic2" {
  name                 = local.topic2_name
  namespace_id         = azurerm_servicebus_namespace.servicebus2.id
  partitioning_enabled = true
}

resource "azurerm_servicebus_subscription" "subscription1" {
  name                = local.sub1_name
  topic_id            = azurerm_servicebus_topic.topic1.id
  max_delivery_count  = 10
  lock_duration       = "PT5M"
  requires_session    = true
}

resource "azurerm_servicebus_subscription" "subscription2" {
  name                = local.sub2_name
  topic_id            = azurerm_servicebus_topic.topic2.id
  max_delivery_count  = 10
  lock_duration       = "PT5M"
  requires_session    = true
}