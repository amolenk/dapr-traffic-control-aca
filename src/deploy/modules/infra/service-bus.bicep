param location string
param uniqueSeed string
param serviceBusName string = 'sb-${uniqueString(uniqueSeed)}'

param keyVaultName string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2021-06-01-preview' = {
  name: serviceBusName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' existing = {
  name: keyVaultName

  resource cosmosKeySecret 'secrets' = {
    name: 'ConnectionStrings--ServiceBus'
    properties: {
      value: 'Endpoint=sb://${serviceBus.name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${listKeys('${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey', serviceBus.apiVersion).primaryKey}'
    }
  }
}
