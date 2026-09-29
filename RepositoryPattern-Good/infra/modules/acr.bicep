// Creates a registry, or just resolves an existing one by name - the caller decides which via
// createNew, and either way gets back the same id/loginServer/name shape. Deploy this module at
// the resource group that should actually contain a NEW registry, or that already contains an
// EXISTING one (see main.bicep's `scope:` on this module's invocation) - they're not always the
// same resource group as the cluster itself.
param location string
param name string
param sku string = 'Basic'
param tags object = {}
param createNew bool

resource newAcr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = if (createNew) {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
  }
}

resource existingAcr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = if (!createNew) {
  name: name
}

output id string = createNew ? newAcr.id : existingAcr.id
output loginServer string = createNew ? newAcr.properties.loginServer : existingAcr.properties.loginServer
output name string = name
