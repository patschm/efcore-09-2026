using 'cosmos.bicep'

param accountName = 'pscosmosdb'
param databaseName = 'webshop'
param referenceMaxThroughputRuPerSecond = 1000
param productsMaxThroughputRuPerSecond = 1000
param vectorDimensions = 1024
param tags = {
  project: 'WebShop'
}
