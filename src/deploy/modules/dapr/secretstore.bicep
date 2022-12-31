param containerAppsEnvironmentName string
param vaultName string
param managedIdentityName string

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppsEnvironmentName

  resource secretstore 'daprComponents@2022-03-01' = {
    name: 'secretstore'
    properties: {
      componentType: 'secretstores.azure.keyvault'
      version: 'v1'
      metadata: [
        {
          name: 'vaultName'
          value: vaultName
        }
        {
          name: 'azureClientId'
          value: managedIdentity.properties.clientId
        }
      ]
      scopes: [
        'finecollectionservice'
      ]
    }
  }
}

output daprSecretStoreName string = containerAppsEnvironment::secretstore.name
