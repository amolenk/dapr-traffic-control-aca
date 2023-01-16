param containerAppsEnvironmentName string
param mqttHost string
param mqttPort int

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'entrycam'
    properties: {
      componentType: 'bindings.mqtt'
      version: 'v1'
      metadata: [
        {
          name: 'url'
          value: 'mqtt://${mqttHost}:${mqttPort}' 
        }
        {
          name: 'topic'
          value: 'trafficcontrol/entrycam'
        }
        {
          name: 'consumerID'
          value: '{uuid}'
        }
      ]
      scopes: [
        'trafficcontrolservice'
      ]
    }
  }
}
