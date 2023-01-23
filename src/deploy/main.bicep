param location string = resourceGroup().location
param uniqueSeed string = '${resourceGroup().id}-eshopondapr'

param deployInfra bool = true
param deployApps bool = true

param sqlAdministratorLogin string = 'server_admin'

@secure()
param sqlAdministratorLoginPassword string = ''

param authClientId string = ''
@secure()
param authClientSecret string = ''

param appInsightsName string = 'appinsights-${uniqueString(uniqueSeed)}'
param containerAppsEnvironmentName string = 'containerappenv-${uniqueString(uniqueSeed)}'
param cosmosAccountName string = 'cosmos-${uniqueString(uniqueSeed)}'
param cosmosCollectionName string = 'VehicleState'
param cosmosDbName string = 'TrafficControl'
param keyVaultName string = 'keyvault-${uniqueString(uniqueSeed)}'
param logAnalyticsWorkspaceName string = 'loganalytics-${uniqueString(uniqueSeed)}'
param managedIdentityName string = 'identity-${uniqueString(uniqueSeed)}'
param serviceBusName string = 'sb-${uniqueString(uniqueSeed)}'
param sqlServerName string = 'sql-${uniqueString(uniqueSeed)}'
param virtualNetworkName string = 'vnet-${uniqueString(uniqueSeed)}'

module infra 'infra.bicep' = if (deployInfra) {
  name: '${deployment().name}-infra'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    appInsightsName: appInsightsName
    containerAppsEnvironmentName: containerAppsEnvironmentName
    cosmosAccountName: cosmosAccountName
    keyVaultName: keyVaultName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
    managedIdentityName:managedIdentityName
    serviceBusName: serviceBusName
    sqlServerName: sqlServerName
    virtualNetworkName: virtualNetworkName
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

module apps 'apps.bicep' = if (deployApps) {
  name: '${deployment().name}-apps'
  params: {
    location: location
    authClientId: authClientId
    authClientSecret: authClientSecret
    containerAppsEnvironmentName: containerAppsEnvironmentName
    cosmosAccountName: cosmosAccountName
    cosmosCollectionName: cosmosCollectionName
    cosmosDbName: cosmosDbName
    keyVaultName: keyVaultName
    managedIdentityName:managedIdentityName
    sqlServerName: sqlServerName
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

output containerAppsEnvironmentDomain string = infra.outputs.containerAppsEnvironmentDomain
output cosmosAccountName string = infra.outputs.cosmosAccountName
output cosmosDbName string = infra.outputs.cosmosDbName
output cosmosCollectionName string = infra.outputs.cosmosCollectionName
output managedIdentityPrincipalId string = infra.outputs.managedIdentityPrincipalId
