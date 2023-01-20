param containerAppsEnvironmentName string

param cosmosDbName string
param cosmosCollectionName string
param cosmosUrl string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'statestore'
    properties: {
      componentType: 'state.azure.cosmosdb'
      version: 'v1'
      secretStoreComponent: 'secretstore'
      metadata: [
        {
          name: 'url'
          value: cosmosUrl
        }
        {
          name: 'masterKey'
          secretRef: 'CosmosDb--PrimaryKey'
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
