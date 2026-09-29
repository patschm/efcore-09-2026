// The Data Collection Rule Association that actually wires this up to the AKS cluster lives in
// main.bicep, not here - it's an extension resource on the cluster (scope: aks), and the cluster
// is created by a sibling module, not this one.
param clusterName string
param location string
param tags object = {}

resource monitorWorkspace 'Microsoft.Monitor/accounts@2023-04-03' = {
  name: '${clusterName}-metrics'
  location: location
  tags: tags
}

resource dataCollectionEndpoint 'Microsoft.Insights/dataCollectionEndpoints@2022-06-01' = {
  name: 'MSProm-${clusterName}'
  location: location
  tags: tags
  kind: 'Linux'
  properties: {}
}

resource dataCollectionRule 'Microsoft.Insights/dataCollectionRules@2022-06-01' = {
  name: 'MSProm-${clusterName}'
  location: location
  tags: tags
  kind: 'Linux'
  properties: {
    dataCollectionEndpointId: dataCollectionEndpoint.id
    dataSources: {
      prometheusForwarder: [
        {
          name: 'PrometheusDataSource'
          streams: ['Microsoft-PrometheusMetrics']
        }
      ]
    }
    destinations: {
      monitoringAccounts: [
        {
          accountResourceId: monitorWorkspace.id
          name: 'MonitoringAccount1'
        }
      ]
    }
    dataFlows: [
      {
        streams: ['Microsoft-PrometheusMetrics']
        destinations: ['MonitoringAccount1']
      }
    ]
  }
}

output monitorWorkspaceId string = monitorWorkspace.id
output dataCollectionRuleId string = dataCollectionRule.id
