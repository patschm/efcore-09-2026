// AKS cluster for WebShop, with the KEDA autoscaler add-on and the Dapr cluster extension
// enabled. Scope is deliberately just the cluster + its natural companions (ACR to hold the six
// service images, Log Analytics for Container Insights, Application Insights/managed-Prometheus/
// Grafana for OpenTelemetry) - it does not deploy the app workloads themselves (Deployments/
// Services/Dapr components/KEDA ScaledObjects), which belong in Helm charts or plain manifests
// applied against this cluster afterwards.
//
// Postgres is NOT provisioned here, matching docker-compose.yml's approach: this project runs
// against an existing Postgres (currently the standalone "postgres-local" container). For AKS
// you'd point the services at a real reachable Postgres - e.g. Azure Database for PostgreSQL
// Flexible Server - via the same ConnectionStrings__* settings docker-compose.yml already uses.
//
// This file is just the orchestrator - see infra/modules/*.bicep for the actual resources. Each
// module is deployed at whatever scope it needs (usually this resource group, but see the ACR
// module below for why that's not always true).

targetScope = 'resourceGroup'

@description('Azure region for every resource.')
param location string = resourceGroup().location

@description('Name of the AKS cluster.')
param clusterName string = 'webshop-aks'

@description('DNS prefix for the cluster API server.')
param dnsPrefix string = clusterName

@description('VM size for the system node pool.')
param systemNodeVmSize string = 'Standard_D2s_v5'

@description('Initial node count for the system pool (autoscaling adjusts this afterwards).')
param systemNodeCount int = 2

@description('Minimum node count the system pool can scale down to.')
param systemNodeMinCount int = 1

@description('Maximum node count the system pool can scale up to. 6, not 4, because 4 D2s_v5 nodes turned out not to be enough headroom once KEDA/Dapr/cert-manager/Container Insights and all six services\' Pods are scheduled together - see infra/README.md\'s cost note before raising this further.')
param systemNodeMaxCount int = 6

@description('Enable the KEDA (event-driven autoscaling) add-on.')
param enableKeda bool = true

@description('Enable AKS\'s managed "app routing" add-on - a managed NGINX ingress controller (ingress class webapprouting.kubernetes.azure.com), needed for k8s/15-web.yaml\'s Ingress and HTTPS via cert-manager (see k8s/03-cert-manager-issuers.yaml).')
param enableAppRoutingAddon bool = true

@description('Enable the Dapr cluster extension.')
param enableDapr bool = true

@description('Run Dapr control-plane pods in high-availability mode (3 replicas instead of 1). Leave off for a small/dev cluster.')
param daprHighAvailability bool = false

@description('Give the cluster ACR pull access - to a newly created registry, or to existingAcrName if set.')
param enableAcr bool = true

@description('Name of an existing Azure Container Registry to reuse instead of creating one - e.g. a registry from another project. Leave empty to create a new one (see acrSku).')
param existingAcrName string = ''

@description('Resource group containing existingAcrName, if it\'s not this deployment\'s own resource group. Ignored when existingAcrName is empty.')
param existingAcrResourceGroupName string = resourceGroup().name

@description('SKU for a newly created registry. Ignored when existingAcrName is set.')
@allowed(['Basic', 'Standard', 'Premium'])
param acrSku string = 'Basic'

@description('Name of the Log Analytics workspace backing Container Insights.')
param logAnalyticsWorkspaceName string = '${clusterName}-logs'

@description('Provision Application Insights as the destination for traces/logs - the OTel Collector deployed in-cluster (see observability/azure/otel-collector.yaml) exports to this via its azuremonitor exporter.')
param enableApplicationInsights bool = true

@description('Enable AKS\'s managed-Prometheus metrics add-on. Uses its own storage (an Azure Monitor Workspace, not Log Analytics) - kept separate from Application Insights because the azuremonitor exporter\'s metrics support is comparatively immature; this is the recommended path for metrics instead.')
param enableManagedPrometheus bool = true

@description('Provision Azure Managed Grafana, pre-wired to the Azure Monitor Workspace, for exploring the Prometheus metrics. Off by default - it has its own ongoing cost, and Application Insights already covers traces/logs without it.')
param enableManagedGrafana bool = false

@description('Tags applied to every resource.')
param tags object = {}

// ACR names must be globally unique and alphanumeric only - derive one deterministically so a
// redeploy targets the same registry instead of trying to create a new one each time. Only used
// when existingAcrName is empty.
var acrGeneratedName = toLower('${replace(clusterName, '-', '')}acr${uniqueString(resourceGroup().id)}')
var acrIsExisting = !empty(existingAcrName)
var acrName = acrIsExisting ? existingAcrName : acrGeneratedName
var acrResourceGroupName = acrIsExisting ? existingAcrResourceGroupName : resourceGroup().name

// Deployed at acrResourceGroupName, not necessarily this template's own resource group - an
// existing registry from another project (existingAcrName) can live anywhere in the subscription.
module acr 'modules/acr.bicep' = if (enableAcr) {
  name: 'acr'
  scope: resourceGroup(acrResourceGroupName)
  params: {
    location: location
    name: acrName
    sku: acrSku
    tags: tags
    createNew: !acrIsExisting
  }
}

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'log-analytics'
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }
}

module aksModule 'modules/aks.bicep' = {
  name: 'aks'
  params: {
    location: location
    clusterName: clusterName
    dnsPrefix: dnsPrefix
    systemNodeVmSize: systemNodeVmSize
    systemNodeCount: systemNodeCount
    systemNodeMinCount: systemNodeMinCount
    systemNodeMaxCount: systemNodeMaxCount
    enableKeda: enableKeda
    enableManagedPrometheus: enableManagedPrometheus
    enableAppRoutingAddon: enableAppRoutingAddon
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

// Bridges back to the cluster the module above created, so extension resources (Dapr, the
// Prometheus association) can attach to it via `scope: aks` - Bicep modules can only hand each
// other outputs, not live resource symbols usable as a `scope`. Named from the clusterName
// parameter directly, not an aksModule output - an extension resource's scope must be
// calculable at the start of the deployment, which a module's runtime output isn't. This also
// means Bicep won't infer that anything scoped to `aks` has to wait for aksModule to finish, so
// daprExtension and dataCollectionRuleAssociation below each add that dependsOn explicitly.
resource aks 'Microsoft.ContainerService/managedClusters@2024-09-01' existing = {
  name: clusterName
}

// The Dapr control plane (sidecar injector, placement service, sentry, operator) as a managed
// cluster extension, rather than `dapr init -k` / a self-managed Helm install - Azure handles
// its lifecycle and upgrades alongside the cluster.
resource daprExtension 'Microsoft.KubernetesConfiguration/extensions@2022-11-01' = if (enableDapr) {
  name: 'dapr'
  scope: aks
  dependsOn: [aksModule]
  properties: {
    extensionType: 'Microsoft.Dapr'
    autoUpgradeMinorVersion: true
    releaseTrain: 'Stable'
    configurationSettings: {
      'global.ha.enabled': string(daprHighAvailability)
    }
  }
}

// Separate from the acr module so creating/resolving the registry is never blocked waiting on
// the cluster - this only needs the cluster's kubelet identity, deployed once both exist.
module acrPullRoleAssignment 'modules/acrPullRoleAssignment.bicep' = if (enableAcr) {
  name: 'acr-pull-role-assignment'
  scope: resourceGroup(acrResourceGroupName)
  params: {
    acrName: acr.outputs.name
    principalId: aksModule.outputs.kubeletPrincipalId
  }
}

module appInsights 'modules/appInsights.bicep' = if (enableApplicationInsights) {
  name: 'app-insights'
  params: {
    name: '${clusterName}-insights'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

module managedPrometheus 'modules/managedPrometheus.bicep' = if (enableManagedPrometheus) {
  name: 'managed-prometheus'
  params: {
    clusterName: clusterName
    location: location
    tags: tags
  }
}

// Links the cluster's Prometheus scraping to the Azure Monitor Workspace above - without this
// association, metrics are collected in-cluster but never actually shipped anywhere.
resource dataCollectionRuleAssociation 'Microsoft.Insights/dataCollectionRuleAssociations@2022-06-01' = if (enableManagedPrometheus) {
  name: 'MSProm-${clusterName}'
  scope: aks
  dependsOn: [aksModule]
  properties: {
    dataCollectionRuleId: managedPrometheus.outputs.dataCollectionRuleId
  }
}

module grafana 'modules/grafana.bicep' = if (enableManagedGrafana) {
  name: 'grafana'
  params: {
    clusterName: clusterName
    location: location
    tags: tags
    monitorWorkspaceId: enableManagedPrometheus ? managedPrometheus.outputs.monitorWorkspaceId : ''
  }
}

output clusterName string = aksModule.outputs.name
output clusterId string = aksModule.outputs.id
output oidcIssuerUrl string = aksModule.outputs.oidcIssuerUrl
output acrLoginServer string = enableAcr ? acr.outputs.loginServer : ''
output getCredentialsCommand string = 'az aks get-credentials --resource-group ${resourceGroup().name} --name ${aksModule.outputs.name}'

@description('Feed this into observability/azure/otel-collector.yaml\'s Secret (see that file) so the in-cluster collector can export traces/logs to Application Insights.')
output appInsightsConnectionString string = enableApplicationInsights ? appInsights.outputs.connectionString : ''
output monitorWorkspaceId string = enableManagedPrometheus ? managedPrometheus.outputs.monitorWorkspaceId : ''
output grafanaEndpoint string = enableManagedGrafana ? grafana.outputs.endpoint : ''
