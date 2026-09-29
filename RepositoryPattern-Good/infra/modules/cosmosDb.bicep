// A single Cosmos DB (Core SQL) account with the two containers the Cosmos persistence layer
// needs - "reference" (shared master/taxonomy data, partitioned by /type) and "products"
// (everything scoped to one product, partitioned by /productId, with a vector index on the
// Embedding item's /vector field). Shapes here must stay in sync with
// BuildingBlocks/Cosmos/CosmosContainerProvisioner.cs, which is the source of truth the running
// app/migration tool actually uses at startup - this module exists so the account itself (and its
// account-level vector-search capability, which has to be set at creation or explicitly enabled
// before the containers can be created - see the README note this was written after) is
// reproducible via `az deployment group create` instead of ad hoc `az cosmosdb create` commands.
param location string
param accountName string
param databaseName string = 'webshop'

@description('Max autoscale RU/s for the "reference" container. Actual usage floats between 10% of this and this value; Azure bills for whatever the container actually scales to each hour.')
param referenceMaxThroughputRuPerSecond int = 1000

@description('Max autoscale RU/s for the "products" container. Bumped above the 1000 default for pscosmosdb to speed up the one-off NDJSON dump import - see Tools/CosmosMigration; safe to scale back down afterward.')
param productsMaxThroughputRuPerSecond int = 1000

@description('Dimensions of the embedding vector stored on Search\'s Embedding documents (products container, /vector path). Must match whatever embedding model produces the vectors.')
param vectorDimensions int = 1024

param tags object = {}

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        // Zone-redundant accounts hit "high demand, capacity unavailable" in more than one region
        // when this was first provisioned by hand - keep this off unless you've confirmed the
        // target region has zonal capacity.
        isZoneRedundant: false
      }
    ]
    // Required up front (or via a separate `az cosmosdb update --capabilities` call before the
    // containers are created) for the products container's VectorEmbeddingPolicy below - a
    // container create/update against an account missing this capability fails with "A Container
    // Vector Policy has been provided, but the capability has not been enabled on your account."
    capabilities: [
      { name: 'EnableNoSQLVectorSearch' }
    ]
    enableFreeTier: false
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

// reference: type="Brand"|"ProductGroup"|"Shop"|"ReviewUser", one logical partition per type.
resource referenceContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: database
  name: 'reference'
  properties: {
    resource: {
      id: 'reference'
      partitionKey: {
        paths: ['/type']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [{ path: '/*' }]
        compositeIndexes: [
          [
            { path: '/type', order: 'ascending' }
            { path: '/parentId', order: 'ascending' }
          ]
        ]
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: referenceMaxThroughputRuPerSecond
      }
    }
  }
}

// products: type="Product"|"SpecValue"|"Embedding"|"Price"|"Review", one partition per product.
// TTL enabled at the container level (off per item unless the item sets its own "ttl") - only
// soft-deleted Review items use it today, via SoftDelete.Patch.
resource productsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  parent: database
  name: 'products'
  properties: {
    resource: {
      id: 'products'
      partitionKey: {
        paths: ['/productId']
        kind: 'Hash'
      }
      defaultTtl: -1
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/vector'
            dataType: 'float32'
            dimensions: vectorDimensions
            distanceFunction: 'cosine'
          }
        ]
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [{ path: '/*' }]
        // Excluded from the default range index and given a dedicated vector index instead -
        // indexing a 1024-float array the normal way would be expensive and pointless.
        excludedPaths: [{ path: '/vector/*' }]
        vectorIndexes: [
          { path: '/vector', type: 'quantizedFlat' }
        ]
        compositeIndexes: [
          [
            { path: '/type', order: 'ascending' }
            { path: '/productGroupId', order: 'ascending' }
          ]
          [
            { path: '/type', order: 'ascending' }
            { path: '/brandId', order: 'ascending' }
          ]
        ]
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: productsMaxThroughputRuPerSecond
      }
    }
  }
}

output accountName string = account.name
output documentEndpoint string = account.properties.documentEndpoint

@description('Fetch the actual key separately - deliberately not an output here: `az cosmosdb keys list --name <accountName> --resource-group <rg> --query primaryMasterKey -o tsv`.')
output keysListCommand string = 'az cosmosdb keys list --name ${account.name} --resource-group ${resourceGroup().name} --query primaryMasterKey -o tsv'
