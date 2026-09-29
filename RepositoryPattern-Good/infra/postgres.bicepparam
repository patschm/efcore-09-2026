using 'postgres.bicep'

param serverName = 'webshop-standalone-pg'
param administratorLogin = 'webshopadmin'
param databaseName = 'webshop'

// Same convention as aca.bicepparam: never a committed secret value.
// Set OPS_POSTGRES_ADMIN_PASSWORD before deploying.
param administratorPassword = readEnvironmentVariable('OPS_POSTGRES_ADMIN_PASSWORD')
