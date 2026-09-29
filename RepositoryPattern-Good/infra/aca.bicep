// Azure Container Apps (ACA) deployment for WebShop - a second, alternative way to run the same
// six containers as infra/main.bicep's AKS cluster, for comparing the two hosting models (see
// project memory's "Deployment targets" note). Deploy this to its OWN resource group, separate
// from AKS's - the two are meant to be able to coexist, not replace each other.
//
// What's genuinely different from AKS here, not just "the same thing in a different service":
//   - Dapr needs no cluster extension/install step - every Container Apps environment has it
//     built in; each app just sets `dapr.enabled: true` (see modules/containerApp.bicep). No
//     dapr.io/enabled annotation, no separate Microsoft.KubernetesConfiguration/extensions
//     resource like main.bicep's daprExtension.
//   - TLS on the default `*.<region>.azurecontainerapps.io` domain is automatic - no cert-manager,
//     no ClusterIssuer, no ACME account (compare k8s/03-cert-manager-issuers.yaml).
//   - Postgres has no in-cluster equivalent (no StatefulSet/PersistentVolume concept here) - it's
//     Azure Database for PostgreSQL Flexible Server from the start (modules/postgresFlexibleServer.bicep),
//     not a later migration like AKS's k8s/04-postgres.yaml was designed to eventually become.
//   - Registry auth uses one user-assigned managed identity + an AcrPull role assignment
//     (identity-based `registries[].identity`), reusing main.bicep's acrPullRoleAssignment module
//     verbatim - conceptually the same as AKS's kubelet identity, just attached to Container Apps
//     instead of node VMs.
//
// Explicitly out of scope for this pass (same spirit as main.bicep's own scoping notes):
//   - OpenTelemetry export. AKS routes OTEL_EXPORTER_OTLP_ENDPOINT at an in-cluster collector
//     (observability/azure/otel-collector.yaml, deployed separately from main.bicep) that forwards
//     to Application Insights - there's no equivalent collector deployed here yet, so
//     OTEL_EXPORTER_OTLP_ENDPOINT is left unset and BuildingBlocks/Api/ObservabilityExtensions.cs's
//     OTLP exporter just retries harmlessly against its localhost default (see that file's own
//     comment on why that's safe). Revisit by running the same otel-collector image as a seventh
//     Container App if/when this matters.
//   - Managed Grafana/Prometheus - AKS-specific add-ons with no direct ACA equivalent; Container
//     Apps' own metrics surface through Azure Monitor/Log Analytics instead (see
//     containerAppsEnvironment.bicep's appLogsConfiguration).

targetScope = 'resourceGroup'

@description('Azure region for every resource.')
param location string = resourceGroup().location

@description('Name of the Container Apps environment.')
param environmentName string = 'webshop-aca-env'

@description('Name of an existing Azure Container Registry to reuse - same registry infra/main.bicep points at, so the same images (see k8s/kustomization.yaml\'s tag scheme) work on both deployment targets unchanged.')
param existingAcrName string = 'psrepo'

@description('Resource group containing existingAcrName.')
param existingAcrResourceGroupName string = 'Dapr'

@description('Shared version+provider tag for the five .NET service images (see k8s/kustomization.yaml\'s header comment for the v<N>-<provider> scheme).')
param appImageTag string = 'v7-cosmos'

@description('Tag for the embedding image - versioned separately since it has no persistence provider of its own and changes far less often.')
param embeddingImageTag string = 'v2'

@description('Name of the Log Analytics workspace backing this environment\'s logs.')
param logAnalyticsWorkspaceName string = '${environmentName}-logs'

@description('Region for the Postgres Flexible Server specifically - separate from `location` because this subscription is restricted from provisioning Flexible Server in westeurope (confirmed via `az postgres flexible-server list-skus --location westeurope`, which comes back with an empty supported-version list and a "restricted from provisioning in this region" reason); northeurope/francecentral/swedencentral are open. Everything else (the Container Apps environment, all six apps, Log Analytics) stays in `location` - only Postgres needs to live elsewhere. Cross-region adds a little latency to Web\'s login/Identity queries only, not to anything on the Catalog/Pricing/Reviews/Search Cosmos path.')
param postgresLocation string = 'northeurope'

@description('Name of the Postgres Flexible Server - globally unique (it forms part of a public DNS name).')
param postgresServerName string = 'webshop-identity-pg'

@description('Administrator login for the Postgres Flexible Server.')
param postgresAdminLogin string = 'webshopadmin'

@description('Administrator password for the Postgres Flexible Server. Pass this at deploy time (az deployment group create -p postgresAdminPassword=...) - never commit a real value in a .bicepparam file, matching k8s/01-secrets.yaml\'s placeholder convention.')
@secure()
param postgresAdminPassword string

@description('The real Cosmos account connection string (see infra/cosmos.bicep) - Catalog/Pricing/Reviews/Search\'s actual datastore. Pass at deploy time, same rule as postgresAdminPassword.')
@secure()
param cosmosConnectionString string

@description('Non-secret - the Cosmos database name (matches infra/cosmos.bicepparam\'s databaseName).')
param cosmosDatabaseName string = 'webshop'

@description('Shared signing key Web mints JWTs with and Reviews validates them with (see Reviews/Api/Program.cs\'s AddJwtBearer). Pass a real random value at deploy time - never the appsettings.json dev default.')
@secure()
param jwtSigningKey string

param jwtIssuer string = 'WebShop.Web'
param jwtAudience string = 'WebShop.Reviews'

@description('Reviewer admin email(s) granted the reviews:administer claim on first login - see Web/Identity/AdminClaimsSeeder.cs.')
param identityAdminEmails array = ['testreviewer@example.com']

param tags object = {
  project: 'WebShop'
}

// One identity shared by every Container App below purely for registry pull - simpler than a
// separate identity per app, and matches AKS's model of one kubelet identity for the whole
// cluster rather than one per Pod.
resource acrPullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${environmentName}-acrpull'
  location: location
  tags: tags
}

module acrPullRoleAssignment 'modules/acrPullRoleAssignment.bicep' = {
  name: 'aca-acr-pull-role-assignment'
  scope: resourceGroup(existingAcrResourceGroupName)
  params: {
    acrName: existingAcrName
    principalId: acrPullIdentity.properties.principalId
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: existingAcrName
  scope: resourceGroup(existingAcrResourceGroupName)
}

module logAnalytics 'modules/logAnalytics.bicep' = {
  name: 'aca-log-analytics'
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: tags
  }
}

module environment 'modules/containerAppsEnvironment.bicep' = {
  name: 'aca-environment'
  params: {
    name: environmentName
    location: location
    tags: tags
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: logAnalytics.outputs.primarySharedKey
  }
}

module postgres 'modules/postgresFlexibleServer.bicep' = {
  name: 'aca-postgres'
  params: {
    location: postgresLocation
    serverName: postgresServerName
    administratorLogin: postgresAdminLogin
    administratorPassword: postgresAdminPassword
  }
}

// Npgsql's Azure Postgres Flexible Server connection needs SSL - Flexible Server enforces it by
// default, unlike the plain in-cluster postgres:pg17 image k8s/04-postgres.yaml runs (no TLS
// termination of its own, so nothing to require).
var identityConnectionString = 'Host=${postgres.outputs.fqdn};Port=5432;Database=${postgres.outputs.databaseName};Username=${postgresAdminLogin};Password=${postgresAdminPassword};Ssl Mode=Require;Trust Server Certificate=true'

var commonAppSettings = [
  { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
]

var cosmosSecretName = 'cosmos-connection-string'
var jwtSecretName = 'jwt-signing-key'

module catalog 'modules/containerApp.bicep' = {
  name: 'aca-catalog'
  params: {
    name: 'catalog'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-catalog:${appImageTag}'
    containerPort: 8080
    daprAppId: 'catalog'
    secrets: { '${cosmosSecretName}': cosmosConnectionString }
    env: concat(commonAppSettings, [{ name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }])
    secretEnv: [{ name: 'CosmosDb__ConnectionString', secretName: cosmosSecretName }]
  }
}

module pricing 'modules/containerApp.bicep' = {
  name: 'aca-pricing'
  params: {
    name: 'pricing'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-pricing:${appImageTag}'
    containerPort: 8080
    daprAppId: 'pricing'
    secrets: { '${cosmosSecretName}': cosmosConnectionString }
    env: concat(commonAppSettings, [{ name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }])
    secretEnv: [{ name: 'CosmosDb__ConnectionString', secretName: cosmosSecretName }]
  }
}

module reviews 'modules/containerApp.bicep' = {
  name: 'aca-reviews'
  params: {
    name: 'reviews'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-reviews:${appImageTag}'
    containerPort: 8080
    daprAppId: 'reviews'
    secrets: {
      '${cosmosSecretName}': cosmosConnectionString
      '${jwtSecretName}': jwtSigningKey
    }
    env: concat(commonAppSettings, [
      { name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }
      { name: 'Jwt__Issuer', value: jwtIssuer }
      { name: 'Jwt__Audience', value: jwtAudience }
    ])
    secretEnv: [
      { name: 'CosmosDb__ConnectionString', secretName: cosmosSecretName }
      { name: 'Jwt__SigningKey', secretName: jwtSecretName }
    ]
  }
}

module search 'modules/containerApp.bicep' = {
  name: 'aca-search'
  params: {
    name: 'search'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-search:${appImageTag}'
    containerPort: 8080
    daprAppId: 'search'
    secrets: { '${cosmosSecretName}': cosmosConnectionString }
    // No Embedding:Url needed - Search/Api/Program.cs's DaprServiceInvocation only falls back to
    // that config key when DAPR_HTTP_PORT is absent, and Dapr is always active here.
    env: concat(commonAppSettings, [{ name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }])
    secretEnv: [{ name: 'CosmosDb__ConnectionString', secretName: cosmosSecretName }]
  }
}

// The odd one out, same reasons as k8s/14-embedding.yaml: Python/llama_cpp.server, not ASP.NET
// Core, so a different port, health path, resource footprint, and startup tolerance, and no
// ASPNETCORE_ENVIRONMENT/CosmosDb__* settings (meaningless to it). Single replica for the same
// reason as that file - each replica holds the full model in memory.
module embedding 'modules/containerApp.bicep' = {
  name: 'aca-embedding'
  params: {
    name: 'embedding'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-embedding:${embeddingImageTag}'
    containerPort: 8000
    daprAppId: 'embedding'
    cpu: '2.0'
    memory: '4Gi'
    minReplicas: 1
    maxReplicas: 1
    probePath: '/v1/models'
    startupPeriodSeconds: 5
    startupFailureThreshold: 60
    livenessPeriodSeconds: 15
    readinessPeriodSeconds: 10
  }
}

module web 'modules/containerApp.bicep' = {
  name: 'aca-web'
  params: {
    name: 'web'
    location: location
    tags: tags
    environmentId: environment.outputs.id
    acrLoginServer: acr.properties.loginServer
    userAssignedIdentityId: acrPullIdentity.id
    image: '${acr.properties.loginServer}/webshop-web:${appImageTag}'
    containerPort: 8080
    externalIngress: true
    daprAppId: 'web'
    secrets: {
      'identity-connection-string': identityConnectionString
      '${jwtSecretName}': jwtSigningKey
    }
    // No Services__Catalog/Pricing/Reviews/Search needed - same reasoning as Search's
    // Embedding__Url above, Web's own DaprServiceInvocation calls only need Dapr's app-id mesh.
    env: concat(commonAppSettings, [
      { name: 'Jwt__Issuer', value: jwtIssuer }
      { name: 'Jwt__Audience', value: jwtAudience }
      { name: 'Identity__AdminEmails__0', value: identityAdminEmails[0] }
    ])
    secretEnv: [
      { name: 'ConnectionStrings__Identity', secretName: 'identity-connection-string' }
      { name: 'Jwt__SigningKey', secretName: jwtSecretName }
    ]
  }
}

output webUrl string = 'https://${web.outputs.fqdn}'
output postgresFqdn string = postgres.outputs.fqdn
