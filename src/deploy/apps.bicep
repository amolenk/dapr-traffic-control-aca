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

module daprPubSubServiceBus 'modules/dapr/pubsub-sb.bicep' = {
  name: '${deployment().name}-dapr-pubsub-sb'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironmentName
  }
}

module daprPubSubCameras 'modules/dapr/pubsub-cameras.bicep' = {
  name: '${deployment().name}-dapr-pubsub-cam'
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
    daprPubSubCameras
    daprPubSubServiceBus
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
    daprPubSubServiceBus
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
    daprPubSubServiceBus
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
    daprPubSubCameras
    daprSecretStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.id
  }
}
