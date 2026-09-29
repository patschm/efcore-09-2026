param name string
param location string
param tags object = {}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

output id string = logAnalytics.id

// Only needed by consumers that can't reference this workspace as a Bicep resource directly (e.g.
// a Container Apps managedEnvironment's logAnalyticsConfiguration, which wants the raw
// customerId/sharedKey pair instead of a resource id) - AKS's Container Insights wiring uses `id`
// above instead and never needed these.
output customerId string = logAnalytics.properties.customerId
@secure()
output primarySharedKey string = logAnalytics.listKeys().primarySharedKey
