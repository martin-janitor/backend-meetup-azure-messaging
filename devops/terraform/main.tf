# Use specific Vesion due to bug with vNet integartions to functin app
# see https://github.com/hashicorp/terraform-provider-azurerm/issues/17930
# Use specific Vesion due to bug with vNet integartions to functin app
# see https://github.com/hashicorp/terraform-provider-azurerm/issues/17930
terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = "~>3.0"
    }
    azuread = {
      source = "hashicorp/azuread"
      version = "~>2.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "rg-common"
    storage_account_name = "mjcommonstorage"
    container_name       = "tfstate"
    key                  = "be_meetup_state.tfstate"
  }
}

provider "azuread" {
  # Configuration options
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
    key_vault {
      purge_soft_delete_on_destroy = true
      purge_soft_deleted_secrets_on_destroy = true
    }
  }
}

resource "azurerm_resource_group" "messaging_rg" {
  name     = local.rg_name
  location = local.location
}