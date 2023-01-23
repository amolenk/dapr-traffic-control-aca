param containerAppsEnvironmentName string
param mqttHost string
param mqttPort int

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'pubsub-cameras'
    properties: {
      componentType: 'pubsub.mqtt'
      version: 'v1'
      metadata: [
        {
          name: 'url'
          value: 'mqtt://${mqttHost}:${mqttPort}' 
        }
        {
          name: 'consumerID'
          value: '{uuid}'
        }
      ]
      scopes: [
        'trafficcontrolservice'
        'simulationgateway'
      ]
    }
  }
}
