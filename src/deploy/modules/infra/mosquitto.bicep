param location string
param containerAppsEnvironmentId string

param mqttPort int = 1883

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'mosquitto'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    template: {
      containers: [
        {
          name: 'mosquitto'
          image: 'amolenk/dapr-trafficcontrol-mosquitto:latest'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: true
        targetPort: mqttPort
        transport: 'tcp'
      }
    }
  }
}

output mqttHost string = containerApp.name
output mqttPort int = mqttPort
