param location string
param containerAppsEnvironmentId string
param managedIdentityName string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'trafficcontrolservice'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}' : {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    template: {
      containers: [
        {
          name: 'trafficcontrolservice'
          image: 'amolenk/dapr-trafficcontrol-trafficcontrolservice:latest'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
    configuration: {
      activeRevisionsMode: 'Single'
      dapr: {
        enabled: true
        appId: 'trafficcontrolservice'
        appPort: 80
      }
    }
  }
}
