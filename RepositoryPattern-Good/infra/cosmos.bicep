// Standalone entry point for the Cosmos DB account backing the Cosmos persistence layer (see
// BuildingBlocks/Cosmos and each context's Infrastructure/Cosmos project). Deliberately separate
// from main.bicep/the AKS deployment - Cosmos isn't wired into any Program.cs yet (Postgres
// remains the only provider actually running), and this account doesn't need to live in the same
// resource group as the cluster. See infra/modules/cosmosDb.bicep for the actual resource shapes.
//
// Deploy with:
//   az deployment group create \
//     --resource-group <rg> \
//     --template-file infra/cosmos.bicep \
//     --parameters infra/cosmos.bicepparam
//
// Then populate it with Tools/CosmosMigration (--import for a dump, or a full Postgres migration
// run - see that project's Program.cs for both modes).

targetScope = 'resourceGroup'

@description('Azure region for the Cosmos account.')
param location string = resourceGroup().location

@description('Name of the Cosmos DB account. Must be globally unique across Azure.')
param accountName string

@description('Name of the SQL (Core) API database.')
param databaseName string = 'webshop'

@description('Max autoscale RU/s for the "reference" container.')
param referenceMaxThroughputRuPerSecond int = 1000

@description('Max autoscale RU/s for the "products" container.')
param productsMaxThroughputRuPerSecond int = 1000

@description('Dimensions of the embedding vector on Search\'s Embedding documents.')
param vectorDimensions int = 1024

@description('Tags applied to the account.')
param tags object = {}

module cosmos 'modules/cosmosDb.bicep' = {
  name: 'cosmos-db'
  params: {
    location: location
    accountName: accountName
    databaseName: databaseName
    referenceMaxThroughputRuPerSecond: referenceMaxThroughputRuPerSecond
    productsMaxThroughputRuPerSecond: productsMaxThroughputRuPerSecond
    vectorDimensions: vectorDimensions
    tags: tags
  }
}

output accountName string = cosmos.outputs.accountName
output documentEndpoint string = cosmos.outputs.documentEndpoint
output keysListCommand string = cosmos.outputs.keysListCommand
