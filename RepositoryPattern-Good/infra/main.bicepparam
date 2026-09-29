using 'main.bicep'

param clusterName = 'webshop-aks'
param systemNodeVmSize = 'Standard_D2s_v5'
param systemNodeCount = 2
param systemNodeMinCount = 1
param systemNodeMaxCount = 6
param enableKeda = true
param enableAppRoutingAddon = true
param enableDapr = true
param daprHighAvailability = false
param enableAcr = true
// Reusing this training subscription's existing "psrepo" registry (resource group "Dapr") rather
// than creating a new empty one - see infra/README.md's ACR section. (Previously pointed at a
// "psrepoo" registry that no longer exists - a stale name/RG, not a real registry.)
param existingAcrName = 'psrepo'
param existingAcrResourceGroupName = 'Dapr'
param acrSku = 'Basic'
param enableApplicationInsights = true
param enableManagedPrometheus = true
param enableManagedGrafana = false
param tags = {
  project: 'WebShop'
}
