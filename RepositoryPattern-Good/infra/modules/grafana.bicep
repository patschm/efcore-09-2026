param clusterName string
param location string
param tags object = {}

@description('Resource id of the Azure Monitor Workspace to wire Grafana up to. Empty skips both the integration and the role assignment (e.g. managed-Prometheus disabled).')
param monitorWorkspaceId string = ''

resource grafana 'Microsoft.Dashboard/grafana@2023-09-01' = {
  name: '${clusterName}-grafana'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    grafanaIntegrations: {
      azureMonitorWorkspaceIntegrations: empty(monitorWorkspaceId) ? [] : [
        {
          azureMonitorWorkspaceResourceId: monitorWorkspaceId
        }
      ]
    }
  }
}

// Resolved from the id string rather than taking the workspace as a module-to-module reference -
// Bicep modules can only hand each other outputs (ids, names, ...), not live resource symbols.
resource monitorWorkspace 'Microsoft.Monitor/accounts@2023-04-03' existing = if (!empty(monitorWorkspaceId)) {
  name: last(split(monitorWorkspaceId, '/'))
}

var monitoringReaderRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '43d0d8ad-25c7-4714-9337-8ba259a9fe05')

resource monitoringReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(monitorWorkspaceId)) {
  name: guid(monitorWorkspaceId, grafana.id, 'MonitoringReader')
  scope: monitorWorkspace
  properties: {
    roleDefinitionId: monitoringReaderRoleDefinitionId
    principalId: grafana.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output endpoint string = grafana.properties.endpoint
