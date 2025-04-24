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
  partition_count     = 32
  message_retention   = 7     # Increased retention to 7 days for better history tracking
  capture_description {
    enabled             = true
    encoding            = "Avro"
    interval_in_seconds = 300  # Capture data every 5 minutes
    size_limit_in_bytes = 314572800  # Approximately 300 MB
    destination {
      name                = "EventHubArchive.AzureBlockBlob"
      archive_name_format = "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}"
      blob_container_name = "eventhubarchive"
      storage_account_id  = azurerm_storage_account.eventhub_storage.id
    }
  }
}

# Second Event Hub for EventGrid
resource "azurerm_eventhub" "eventhub2_eventgrid" {
  name                = local.eh2_eventgrid_name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
  partition_count     = 32
  message_retention   = 7  
  capture_description {
    enabled             = true
    encoding            = "Avro"
    interval_in_seconds = 300
    size_limit_in_bytes = 314572800
    destination {
      name                = "EventHubArchive.AzureBlockBlob"
      archive_name_format = "{Namespace}/{EventHub}/{PartitionId}/{Year}/{Month}/{Day}/{Hour}/{Minute}/{Second}"
      blob_container_name = "eventhubarchive"
      storage_account_id  = azurerm_storage_account.eventhub_storage.id
    }
  }
}

# Third Event Hub for Replication
resource "azurerm_eventhub" "eventhub3_replication" {
  name                = local.eh3_replication_name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
  partition_count     = 32
  message_retention   = 7  
  # No capture configuration for the replication event hub
}

resource "azurerm_eventhub_consumer_group" "backend_meetup_consumer" {
  name                = "backend-meetup-consumer"
  eventhub_name       = azurerm_eventhub.eventhub1.name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
}

resource "azurerm_eventhub_consumer_group" "eventgrid_consumer" {
  name                = "eventgrid-consumer"
  eventhub_name       = azurerm_eventhub.eventhub2_eventgrid.name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
}

# Consumer group for replication
resource "azurerm_eventhub_consumer_group" "replication_consumer" {
  name                = "consumer-replication"
  eventhub_name       = azurerm_eventhub.eventhub3_replication.name
  namespace_name      = azurerm_eventhub_namespace.eventhub_ns.name
  resource_group_name = azurerm_resource_group.messaging_rg.name
}

# Consumer group for retrieving messages by enqueue time
resource "azurerm_eventhub_consumer_group" "enqueue_time_consumer" {
  name                = local.enqueue_time_consumer_name
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