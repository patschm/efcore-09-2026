// A standalone Azure Database for PostgreSQL Flexible Server - independent of both deployment
// targets (AKS runs its own in-cluster Postgres, see k8s/04-postgres.yaml; infra/aca.bicep
// provisions its OWN Flexible Server as part of that deployment). This file exists so Postgres
// can be created/restored/deleted as its own lifecycle, outside of either cluster - e.g. for the
// WPF ops console's "Postgres" tab, which manages it independently of "set up AKS"/"set up ACA".
//
// Just wraps modules/postgresFlexibleServer.bicep - see that file for the resource itself and why
// its choices (Burstable tier, a public "allow Azure services" firewall rule rather than a VNet
// rule) are what they are.

targetScope = 'resourceGroup'

// Defaults to northeurope, not resourceGroup().location - this subscription is restricted from
// provisioning Postgres Flexible Server in westeurope specifically (confirmed via `az postgres
// flexible-server list-skus --location westeurope`, see infra/aca.bicep's postgresLocation param
// for the same finding). Override if deploying under a subscription without that restriction.
param location string = 'northeurope'
param serverName string = 'webshop-standalone-pg'
param administratorLogin string = 'webshopadmin'

@secure()
param administratorPassword string

param databaseName string = 'webshop'
param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'
param storageSizeGB int = 32
param postgresVersion string = '16'

module postgres 'modules/postgresFlexibleServer.bicep' = {
  name: 'standalone-postgres'
  params: {
    location: location
    serverName: serverName
    administratorLogin: administratorLogin
    administratorPassword: administratorPassword
    databaseName: databaseName
    skuName: skuName
    skuTier: skuTier
    storageSizeGB: storageSizeGB
    postgresVersion: postgresVersion
  }
}

output fqdn string = postgres.outputs.fqdn
output databaseName string = postgres.outputs.databaseName
