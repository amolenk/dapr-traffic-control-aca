param location string = resourceGroup().location
param uniqueSeed string = '${resourceGroup().id}-eshopondapr'

param containerAppsEnvironmentName string
param logAnalyticsWorkspaceName string
param appInsightsName string
param cosmosAccountName string
param keyVaultName string
param managedIdentityName string
param serviceBusName string
param sqlServerName string
param virtualNetworkName string

@secure()
param sqlAdministratorLoginPassword string

////////////////////////////////////////////////////////////////////////////////
// Infrastructure
////////////////////////////////////////////////////////////////////////////////

module managedIdentity 'modules/infra/managed-identity.bicep' = {
  name: '${deployment().name}-infra-managed-identity'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    managedIdentityName: managedIdentityName
  }
}

module virtualNetwork 'modules/infra/virtual-network.bicep' = {
  name: '${deployment().name}-infra-vnet'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    virtualNetworkName: virtualNetworkName
  }
}

module containerAppsEnvironment 'modules/infra/container-apps-env.bicep' = {
  name: '${deployment().name}-infra-container-app-env'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    vnetName: virtualNetworkName
    containerAppsEnvironmentName: containerAppsEnvironmentName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    appInsightsName: appInsightsName
    infraSubnetName: virtualNetwork.outputs.infraSubnetName
  }
}

module keyVault 'modules/infra/key-vault.bicep' = {
  name: '${deployment().name}-infra-key-vault'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    keyVaultName: keyVaultName
    managedIdentityName: managedIdentity.outputs.managedIdentityName
  }
}

module cosmosDb 'modules/infra/cosmos-db.bicep' = {
  name: '${deployment().name}-infra-cosmos-db'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    cosmosAccountName: cosmosAccountName
  }
}

module serviceBus 'modules/infra/service-bus.bicep' = {
  name: '${deployment().name}-infra-service-bus'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    serviceBusName: serviceBusName
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module sqlServer 'modules/infra/sql-server.bicep' = {
  name: '${deployment().name}-infra-sql-server'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    sqlServerName: sqlServerName
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

output containerAppsEnvironmentDomain string = containerAppsEnvironment.outputs.domain
output cosmosAccountName string = cosmosAccountName
output cosmosDbName string = cosmosDb.outputs.cosmosDbName
output cosmosCollectionName string = cosmosDb.outputs.cosmosCollectionName
output managedIdentityPrincipalId string = managedIdentity.outputs.principalId
