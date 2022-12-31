param location string
param uniqueSeed string
param sqlServerName string = 'sql-${uniqueString(uniqueSeed)}'

param sqlAdministratorLogin string = 'server_admin'
@secure()
param sqlAdministratorLoginPassword string

resource sqlServer 'Microsoft.Sql/servers@2021-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorLoginPassword
  }

  resource sqlServerFirewall 'firewallRules@2021-05-01-preview' = {
    name: 'AllowAllWindowsAzureIps'
    properties: {
      // Allow Azure services and resources to access this server
      startIpAddress: '0.0.0.0'
      endIpAddress: '0.0.0.0'
    }
  }
}

output sqlServerName string = sqlServerName
output sqlAdministratorLogin string = sqlAdministratorLogin
