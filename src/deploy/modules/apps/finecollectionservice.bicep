param location string
param containerAppsEnvironmentId string
param managedIdentityName string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource containerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'finecollectionservice'
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
          name: 'finecollectionservice'
          image: 'amolenk/dapr-trafficcontrol-finecollectionservice:latest'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
    configuration: {
      activeRevisionsMode: 'single'
      dapr: {
        enabled: true
        appId: 'finecollectionservice'
        appPort: 443
      }
      ingress: {
        external: true
        targetPort: 443
        // allowInsecure: false
      }
    }
  }
}
