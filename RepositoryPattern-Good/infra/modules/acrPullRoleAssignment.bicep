// Kept separate from acr.bicep so creating the registry is never blocked waiting on the AKS
// cluster to finish - this module needs the cluster's kubelet identity, so it's deployed after
// both exist, but the registry itself doesn't have to be. Deployed at the registry's own
// resource group scope (see main.bicep), same as acr.bicep - it may not be the cluster's own
// resource group (e.g. reusing a registry from another project via existingAcrName).
param acrName string
param principalId string

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: acrName
}

var acrPullRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, principalId, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
