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

# Storage account for Event Hub capture
resource "azurerm_storage_account" "eventhub_storage" {
  name                     = "storehbackendmeetup"
  resource_group_name      = azurerm_resource_group.messaging_rg.name
  location                 = local.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
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