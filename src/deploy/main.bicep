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
    runtimeSubnetName: virtualNetwork.outputs.runtimeSubnetName
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

// module cosmos 'modules/infra/cosmos-db.bicep' = {
//   name: '${deployment().name}-infra-cosmos-db'
//   params: {
//     location: location
//     uniqueSeed: uniqueSeed
//   }
// }

// module serviceBus 'modules/infra/service-bus.bicep' = {
//   name: '${deployment().name}-infra-service-bus'
//   params: {
//     location: location
//     uniqueSeed: uniqueSeed
//   }
// }

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

// module daprPubSub 'modules/dapr/pubsub.bicep' = {
//   name: '${deployment().name}-dapr-pubsub'
//   params: {
//     containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
//     serviceBusConnectionString: serviceBus.outputs.connectionString
//   }
// }

module daprSecretStore 'modules/dapr/secretstore.bicep' = {
  name: '${deployment().name}-dapr-secretstore'
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    managedIdentityName: managedIdentity.outputs.managedIdentityName
    vaultName: keyVault.outputs.keyVaultName
  }
}

// module daprStateStore 'modules/dapr/statestore.bicep' = {
//   name: '${deployment().name}-dapr-statestore'
//   params: {
//     containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
//     cosmosDbName: cosmos.outputs.cosmosDbName
//     cosmosCollectionName: cosmos.outputs.cosmosCollectionName
//     cosmosUrl: cosmos.outputs.cosmosUrl
//     cosmosKey: cosmos.outputs.cosmosKey
//   }
// }

////////////////////////////////////////////////////////////////////////////////
// Container apps
////////////////////////////////////////////////////////////////////////////////

// module basketApi 'modules/apps/basket-api.bicep' = {
//   name: '${deployment().name}-app-basket-api'
//   dependsOn: [
//     daprPubSub
//     daprStateStore
//     seq
//   ]
//   params: {
//     location: location
//     seqFqdn: seq.outputs.fqdn
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//   }
// }

// module blazorClient 'modules/apps/blazor-client.bicep' = {
//   name: '${deployment().name}-app-blazor-client'
//   dependsOn: [
//     seq
//   ]
//   params: {
//     location: location
//     seqFqdn: seq.outputs.fqdn
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//   }
// }

module fineCollectionService 'modules/apps/finecollectionservice.bicep' = {
  name: '${deployment().name}-app-finecollection'
  dependsOn: [
    daprSecretStore
    sqlServer
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

module trafficControlUI 'modules/apps/trafficcontrolui.bicep' = {
  name: '${deployment().name}-app-ui'
  dependsOn: [
    daprSecretStore
    sqlServer
  ]
  params: {
    location: location
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    managedIdentityName: managedIdentity.outputs.managedIdentityName
    authClientId: authClientId
    authClientSecret: authClientSecret
  }
}

// module identityApi 'modules/apps/identity-api.bicep' = {
//   name: '${deployment().name}-app-identity-api'
//   dependsOn: [
//     seq
//   ]
//   params: {
//     location: location
//     seqFqdn: seq.outputs.fqdn
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//     identityDbConnectionString: sqlServer.outputs.identityDbConnectionString
//   }
// }

// module orderingApi 'modules/apps/ordering-api.bicep' = {
//   name: '${deployment().name}-app-ordering-api'
//   dependsOn: [
//     daprPubSub
//     daprStateStore
//     seq
//   ]
//   params: {
//     location: location
//     seqFqdn: seq.outputs.fqdn
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//     orderingDbConnectionString: sqlServer.outputs.identityDbConnectionString
//   }
// }

// module paymentApi 'modules/apps/payment-api.bicep' = {
//   name: '${deployment().name}-app-payment-api'
//   dependsOn: [
//     daprPubSub
//     seq
//   ]
//   params: {
//     location: location
//     seqFqdn: seq.outputs.fqdn
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//   }
// }

// module seq 'modules/apps/seq.bicep' = {
//   name: '${deployment().name}-app-seq'
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//   }
// }

// module webshoppingAgg 'modules/apps/webshopping-agg.bicep' = {
//   name: '${deployment().name}-app-webshopping-agg'
//   dependsOn: [
//     seq
//   ]
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//     seqFqdn: seq.outputs.fqdn
//   }
// }

// module webshoppingGW 'modules/apps/webshopping-gw.bicep' = {
//   name: '${deployment().name}-app-webshopping-gw'
//   dependsOn: [
//     seq
//   ]
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//   }
// }

// module webstatus 'modules/apps/webstatus.bicep' = {
//   name: '${deployment().name}-app-webstatus'
//   dependsOn: [
//     seq
//   ]
//   params: {
//     location: location
//     containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
//     containerAppsEnvironmentDomain: containerAppsEnvironment.outputs.domain
//   }
// }
