param location string
param uniqueSeed string
param managedIdentityName string

param keyVaultName string = 'keyvault-${uniqueString(uniqueSeed)}'
param tenantId string = subscription().tenantId
param skuName string = 'standard'

resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenantId
    enableRbacAuthorization: true
    sku: {
      name: skuName
      family: 'A'
    }
  }

  resource licenseKeySecret 'secrets' = {
    name: 'FineCalculator--LicenseKey'
    properties: {
      value: 'HX783-K2L7V-CRJ4A-5PN1G'
    }
  }

  resource smtpUserSecret 'secrets' = {
    name: 'Smtp--User'
    properties: {
      value: '_username'
    }
  }

  resource smtpPasswordSecret 'secrets' = {
    name: 'Smtp--Password'
    properties: {
      value: '_password'
    }
  }
}

@description('This is the built-in Key Vault Secrets User role.')
resource keyVaultSecretsUserRoleDefinition 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  name: '4633458b-17de-408a-b874-0445c86b69e6'
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: managedIdentityName
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, keyVaultSecretsUserRoleDefinition.id)
  properties: {
    roleDefinitionId: keyVaultSecretsUserRoleDefinition.id
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultName string = keyVault.name
