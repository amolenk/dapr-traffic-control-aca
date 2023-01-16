param location string
param uniqueSeed string

param virtualNetworkName string = 'vnet-${uniqueString(uniqueSeed)}'
param infraSubnetName string = 'Infrastructure'
param runtimeSubnetName string = 'Runtime'

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2022-05-01' = {
  name: virtualNetworkName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: infraSubnetName
        properties: {
          addressPrefix: '10.0.16.0/21'
        }
      }
      {
        name: runtimeSubnetName
        properties: {
          addressPrefix: '10.0.8.0/21'
        }
      }
    ]
  }
}

output vnetName string = virtualNetworkName
output infraSubnetName string = infraSubnetName
