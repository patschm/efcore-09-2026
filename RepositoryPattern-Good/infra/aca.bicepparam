using 'aca.bicep'

param environmentName = 'webshop-aca-env'
param existingAcrName = 'psrepo'
param existingAcrResourceGroupName = 'Dapr'
param appImageTag = 'v7-cosmos'
param embeddingImageTag = 'v2'
param postgresServerName = 'webshop-identity-pg'
param postgresAdminLogin = 'webshopadmin'
param cosmosDatabaseName = 'webshop'
param jwtIssuer = 'WebShop.Web'
param jwtAudience = 'WebShop.Reviews'
param identityAdminEmails = ['testreviewer@example.com']
param tags = {
  project: 'WebShop'
}

// Real secret values are never committed here - readEnvironmentVariable() pulls them from the
// shell environment at deploy time instead, the same convention docker-compose.yml already uses
// for WEBSHOP_COSMOS_CONNECTION (see .gitignore). Set these three before deploying:
//   ACA_POSTGRES_ADMIN_PASSWORD, ACA_COSMOS_CONNECTION_STRING, ACA_JWT_SIGNING_KEY
param postgresAdminPassword = readEnvironmentVariable('ACA_POSTGRES_ADMIN_PASSWORD')
param cosmosConnectionString = readEnvironmentVariable('ACA_COSMOS_CONNECTION_STRING')
param jwtSigningKey = readEnvironmentVariable('ACA_JWT_SIGNING_KEY')
