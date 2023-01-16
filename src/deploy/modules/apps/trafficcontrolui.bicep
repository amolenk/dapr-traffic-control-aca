param location string
param containerAppsEnvironmentId string
param managedIdentityName string
param authClientId string
@secure()
param authClientSecret string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource containerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'trafficcontrolui'
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
          name: 'trafficcontrolui'
          image: 'amolenk/dapr-trafficcontrol-ui:latest'
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
        appId: 'trafficcontrolui'
        appPort: 80
      }
      ingress: {
        external: true
        targetPort: 80
      }
      secrets: [
        {
          name: 'microsoft-provider-authentication-secret'
          value: authClientSecret
        }
      ]
    }
  }

  resource auth 'authConfigs@2022-06-01-preview' = {
    name: 'current'
    properties: {
      platform: {
        enabled: true
      }
      globalValidation: {
        unauthenticatedClientAction: 'AllowAnonymous'
      }
      identityProviders: {
        azureActiveDirectory: {
          registration: {
            openIdIssuer: 'https://sts.windows.net/${subscription().tenantId}/v2.0'
            clientId: authClientId
            clientSecretSettingName: 'microsoft-provider-authentication-secret'
          }
          validation: {
            allowedAudiences: [
              'api://${authClientId}'
            ]
          }
        }
      }
      login: {
        preserveUrlFragmentsForLogins: false
      }
    }
  }
}
