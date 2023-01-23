param location string

param containerAppsEnvironmentName string
param cosmosAccountName string
param cosmosCollectionName string
param cosmosDbName string
param keyVaultName string
param managedIdentityName string
param sqlServerName string

param sqlAdministratorLogin string
@secure()
param sqlAdministratorLoginPassword string

param authClientId string
@secure()
param authClientSecret string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2021-04-15' existing = {
  name: cosmosAccountName
}

////////////////////////////////////////////////////////////////////////////////
// Dapr components
////////////////////////////////////////////////////////////////////////////////

module daprSecretStore 'modules/dapr/secretstore.bicep' = {
  name: '${deployment().name}-dapr-secretstore'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
    managedIdentityName: managedIdentityName
    vaultName: keyVaultName
  }
}

module daprStateStore 'modules/dapr/statestore.bicep' = {
  name: '${deployment().name}-dapr-statestore'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
    cosmosUrl: cosmosAccount.properties.documentEndpoint
    cosmosDbName: cosmosDbName
    cosmosCollectionName: cosmosCollectionName
  }
}

module daprPubSub 'modules/dapr/pubsub.bicep' = {
  name: '${deployment().name}-dapr-pubsub'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
  }
}

module daprEntryCamBinding 'modules/dapr/entrycam.bicep' = {
  name: '${deployment().name}-dapr-entrycam'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
    mqttHost: mosquitto.outputs.mqttHost
    mqttPort: mosquitto.outputs.mqttPort
  }
}

module daprExitCamBinding 'modules/dapr/exitcam.bicep' = {
  name: '${deployment().name}-dapr-exitcam'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
    mqttHost: mosquitto.outputs.mqttHost
    mqttPort: mosquitto.outputs.mqttPort
  }
}

module daprSendMailBinding 'modules/dapr/sendmail.bicep' = {
  name: '${deployment().name}-dapr-sendmail'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
    smtpHost: maildev.outputs.smtpHost
    smtpPort: maildev.outputs.smptPort
  }
}

////////////////////////////////////////////////////////////////////////////////
// Container apps
////////////////////////////////////////////////////////////////////////////////

module mosquitto 'modules/apps/mosquitto.bicep' = {
  name: '${deployment().name}-infra-mosquitto'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
  }
}

module maildev 'modules/apps/maildev.bicep' = {
  name: '${deployment().name}-infra-maildev'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
  }
}

module trafficControlService 'modules/apps/trafficcontrolservice.bicep' = {
  name: '${deployment().name}-app-trafficcontrol'
  dependsOn: [
    daprEntryCamBinding
    daprExitCamBinding
    daprPubSub
    daprSecretStore
    daprStateStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
    managedIdentityName: managedIdentityName
  }
}

module vehicleRegistrationService 'modules/apps/vehicleregistrationservice.bicep' = {
  name: '${deployment().name}-app-vehicleregistration'
  dependsOn: [
    daprSecretStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
    managedIdentityName: managedIdentityName
  }
}

module fineCollectionService 'modules/apps/finecollectionservice.bicep' = {
  name: '${deployment().name}-app-finecollection'
  dependsOn: [
    daprPubSub
    daprSecretStore
    daprSendMailBinding
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
    managedIdentityName: managedIdentityName
    keyVaultName: keyVaultName
    sqlServerName: sqlServerName
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

module trafficControlUI 'modules/apps/trafficcontrolui.bicep' = {
  name: '${deployment().name}-app-ui'
  dependsOn: [
    daprPubSub
    daprSecretStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
    managedIdentityName: managedIdentityName
    authClientId: authClientId
    authClientSecret: authClientSecret
  }
}

module simulationGateway 'modules/apps/simulationgateway.bicep' = {
  name: '${deployment().name}-app-simgw'
  dependsOn: [
    daprEntryCamBinding
    daprExitCamBinding
    daprSecretStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
  }
}
