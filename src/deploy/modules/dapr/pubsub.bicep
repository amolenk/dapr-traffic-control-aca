param containerAppsEnvironmentName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'pubsub'
    properties: {
      componentType: 'pubsub.azure.servicebus'
      version: 'v1'
      secretStoreComponent: 'secretstore'
      metadata: [
        {
          name: 'connectionString'
          secretRef: 'ConnectionStrings--ServiceBus'
        }
      ]
      scopes: [
        'finecollectionservice'
        'trafficcontrolservice'
        'trafficcontrolui'
      ]
    }
  }
}
