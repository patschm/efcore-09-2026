// The ACA equivalent of "the cluster" in infra/main.bicep - a Managed Environment is the boundary
// Container Apps within it share (virtual network, Dapr component scoping, log destination).
// Dapr itself needs NO separate extension/install step here, unlike AKS's `daprExtension`
// (Microsoft.KubernetesConfiguration/extensions in main.bicep) - it's a built-in capability of
// every Container Apps environment; each Container App just opts in via its own `dapr` block
// (see modules/containerApp.bicep).
param name string
param location string
param tags object = {}
param logAnalyticsCustomerId string
@secure()
param logAnalyticsSharedKey string

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
    // Consumption-only environment (no dedicated workload profiles) - the cheapest option and
    // the one that supports scale-to-zero, which Standard/Dedicated workload profiles don't do
    // as freely. Fine for this project's traffic; a workload profile would only matter for GPU
    // node access or guaranteed (non-shared) compute, neither of which applies here.
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

output id string = environment.id
output defaultDomain string = environment.properties.defaultDomain
