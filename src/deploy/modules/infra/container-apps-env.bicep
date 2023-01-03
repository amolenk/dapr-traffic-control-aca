param location string
param uniqueSeed string
param containerAppsEnvironmentName string = 'containerappenv-${uniqueString(uniqueSeed)}'
param logAnalyticsWorkspaceName string = 'loganalytics-${uniqueString(uniqueSeed)}'
param appInsightsName string = 'appinsights-${uniqueString(uniqueSeed)}'
param vnetName string
param infraSubnetName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: { 
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    daprAIInstrumentationKey: appInsights.properties.InstrumentationKey
    vnetConfiguration: {
//      internal: false
      infrastructureSubnetId: resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, infraSubnetName)
//      runtimeSubnetId: resourceId('Microsoft.Network/virtualNetworks/subnets', vnetName, runtimeSubnetName)
    }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironmentName
output domain string = containerAppsEnvironment.properties.defaultDomain
