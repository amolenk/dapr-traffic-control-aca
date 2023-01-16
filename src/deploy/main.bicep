param location string = resourceGroup().location
param uniqueSeed string = '${resourceGroup().id}-${deployment().name}'

@secure()
param sqlAdministratorLoginPassword string

param authClientId string
@secure()
param authClientSecret string

////////////////////////////////////////////////////////////////////////////////
// Infrastructure
////////////////////////////////////////////////////////////////////////////////

module managedIdentity 'modules/infra/managed-identity.bicep' = {
  name: '${deployment().name}-infra-managed-identity'
  params: {
    location: location
    uniqueSeed: uniqueSeed
  }
}

module virtualNetwork 'modules/infra/virtual-network.bicep' = {
  name: '${deployment().name}-infra-vnet'
  params: {
    location: location
    uniqueSeed: uniqueSeed
  }
}

module containerAppsEnvironment 'modules/infra/container-apps-env.bicep' = {
  name: '${deployment().name}-infra-container-app-env'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    vnetName: virtualNetwork.outputs.vnetName
    infraSubnetName: virtualNetwork.outputs.infraSubnetName
  }
}

module keyVault 'modules/infra/key-vault.bicep' = {
  name: '${deployment().name}-infra-key-vault'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    managedIdentityName: managedIdentity.outputs.managedIdentityName
  }
}

module cosmosDb 'modules/infra/cosmos-db.bicep' = {
  name: '${deployment().name}-infra-cosmos-db'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module serviceBus 'modules/infra/service-bus.bicep' = {
  name: '${deployment().name}-infra-service-bus'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    keyVaultName: keyVault.outputs.keyVaultName
  }
}

module sqlServer 'modules/infra/sql-server.bicep' = {
  name: '${deployment().name}-infra-sql-server'
  params: {
    location: location
    uniqueSeed: uniqueSeed
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

////////////////////////////////////////////////////////////////////////////////
// Dapr components
////////////////////////////////////////////////////////////////////////////////

module daprSecretStore 'modules/dapr/secretstore.bicep' = {
  name: '${deployment().name}-dapr-secretstore'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    managedIdentityName: managedIdentity.outputs.managedIdentityName
    vaultName: keyVault.outputs.keyVaultName
  }
}

module daprStateStore 'modules/dapr/statestore.bicep' = {
  name: '${deployment().name}-dapr-statestore'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    cosmosUrl: cosmosDb.outputs.cosmosUrl
    cosmosDbName: cosmosDb.outputs.cosmosDbName
    cosmosCollectionName: cosmosDb.outputs.cosmosCollectionName
  }
}

module daprPubSub 'modules/dapr/pubsub.bicep' = {
  name: '${deployment().name}-dapr-pubsub'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
  }
}

module daprEntryCamBinding 'modules/dapr/entrycam.bicep' = {
  name: '${deployment().name}-dapr-entrycam'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    mqttHost: mosquitto.outputs.mqttHost
    mqttPort: mosquitto.outputs.mqttPort
  }
}

module daprExitCamBinding 'modules/dapr/exitcam.bicep' = {
  name: '${deployment().name}-dapr-exitcam'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    mqttHost: mosquitto.outputs.mqttHost
    mqttPort: mosquitto.outputs.mqttPort
  }
}

////////////////////////////////////////////////////////////////////////////////
// Container apps
////////////////////////////////////////////////////////////////////////////////

module mosquitto 'modules/infra/mosquitto.bicep' = {
  name: '${deployment().name}-infra-mosquitto'
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
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
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    managedIdentityName: managedIdentity.outputs.managedIdentityName
  }
}

// module vehicleRegistrationService 'modules/apps/vehicleregistrationservice.bicep' = {
//   name: '${deployment().name}-app-vehicleregistration'
//   dependsOn: [
//     daprSecretStore
//   ]
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     managedIdentityName: managedIdentity.outputs.managedIdentityName
//   }
// }

module fineCollectionService 'modules/apps/finecollectionservice.bicep' = {
  name: '${deployment().name}-app-finecollection'
  dependsOn: [
    daprSecretStore
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    managedIdentityName: managedIdentity.outputs.managedIdentityName
    keyVaultName: keyVault.outputs.keyVaultName
    sqlServerName: sqlServer.outputs.sqlServerName
    sqlAdministratorLogin: sqlServer.outputs.sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
  }
}

// module trafficControlUI 'modules/apps/trafficcontrolui.bicep' = {
//   name: '${deployment().name}-app-ui'
//   dependsOn: [
//     daprSecretStore
//     sqlServer
//   ]
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     managedIdentityName: managedIdentity.outputs.managedIdentityName
//     authClientId: authClientId
//     authClientSecret: authClientSecret
//   }
// }

output containerAppsEnvironmentDomain string = containerAppsEnvironment.outputs.domain
