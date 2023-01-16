param location string
param containerAppsEnvironmentId string

param smtpPort int = 1025

resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'maildev'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    template: {
      containers: [
        {
          name: 'maildev'
          image: 'maildev/maildev:2.0.5'
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
        targetPort: smtpPort
        exposedPort: smtpPort
        transport: 'tcp'
      }
    }
  }
}

output smptPort int = smtpPort
