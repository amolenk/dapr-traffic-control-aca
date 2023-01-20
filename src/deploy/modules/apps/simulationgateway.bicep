param location string
param containerAppsEnvironmentId string

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'simulationgateway'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    template: {
      containers: [
        {
          name: 'simulationgateway'
          image: 'amolenk/dapr-trafficcontrol-simulationgateway:latest'
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
        allowInsecure: true
        targetPort: 80
        transport: 'http'
      }
      dapr: {
        enabled: true
        appId: 'simulationgateway'
        appPort: 80
      }
    }
  }
}
