param containerAppsEnvironmentName string
param managedIdentityName string
param cosmosDbName string
param cosmosCollectionName string
param cosmosUrl string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'statestore'
    properties: {
      componentType: 'state.azure.cosmosdb'
      version: 'v1'
      metadata: [
        {
          name: 'azureClientId'
          value: managedIdentity.properties.clientId
        }
        {
          name: 'url'
          value: cosmosUrl
        }
        {
          name: 'database'
          value: cosmosDbName
        }
        {
          name: 'collection'
          value: cosmosCollectionName
        }
      ]
      scopes: [
        'trafficcontrolservice'
      ]
    }
  }
}
