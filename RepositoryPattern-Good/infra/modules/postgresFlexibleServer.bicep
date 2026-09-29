// Azure Database for PostgreSQL Flexible Server - the ACA equivalent of k8s/04-postgres.yaml's
// in-cluster StatefulSet. Container Apps has no persistent-volume/StatefulSet concept of its own
// (a scaled-to-zero or rescheduled replica has no durable local disk), so unlike AKS this is a
// real managed database from the start, not a choice made later - see project memory's
// "Deployment targets" note anticipating exactly this.
//
// Only Web's ASP.NET Identity data lives here now (Catalog/Pricing/Reviews/Search run on Cosmos -
// see infra/cosmos.bicep) - a single small database, no read replicas/HA tier needed for what
// this project actually uses it for.
param location string
param serverName string
param administratorLogin string
@secure()
param administratorPassword string
param databaseName string = 'webshop'

@description('Burstable is the cheapest tier with real compute (vs. the free/limited tier) - this database serves login/session data for a demo app, not production traffic.')
param skuName string = 'Standard_B1ms'
param skuTier string = 'Burstable'
param storageSizeGB int = 32
param postgresVersion string = '16'

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    // No highAvailability block - ZoneRedundant/SameZone HA roughly doubles cost for a database
    // this project doesn't need surviving a zone outage for.
  }
}

// Container Apps' Consumption environment has no VNet integration by default - egress happens
// from a shared, unpredictable set of Azure IPs, not one fixed address you could allow-list.
// "0.0.0.0-0.0.0.0" is Azure's own documented convention for "allow any Azure-internal service",
// not a real internet-facing hole (Postgres still requires its own login+password on top of this;
// this rule only gets a connection attempt as far as the TCP handshake). Tighten this to a VNet
// rule instead if this environment ever moves to VNet integration.
resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: server
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: server
  name: databaseName
}

output fqdn string = server.properties.fullyQualifiedDomainName
output databaseName string = database.name
