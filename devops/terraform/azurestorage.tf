# Storage account for Event Hub capture and queue storage
resource "azurerm_storage_account" "eventhub_storage" {
  name                     = "storehbackendmeetup"
  resource_group_name      = azurerm_resource_group.messaging_rg.name
  location                 = local.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Storage queue for meetup
resource "azurerm_storage_queue" "meetup_queue" {
  name                 = local.meetup_queue_name
  storage_account_name = azurerm_storage_account.eventhub_storage.name
}