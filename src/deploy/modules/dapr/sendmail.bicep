param containerAppsEnvironmentName string
param smtpHost string
param smtpPort int

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' existing = {
  name: containerAppsEnvironmentName

  resource daprComponent 'daprComponents@2022-06-01-preview' = {
    name: 'sendmail'
    properties: {
      componentType: 'bindings.smtp'
      version: 'v1'
      secretStoreComponent: 'secretstore'
      metadata: [
        {
          name: 'host'
          value: smtpHost
        }
        {
          name: 'port'
          value: string(smtpPort)
        }
        {
          name: 'user'
          secretRef: 'Smtp--User'
        }
        {
          name: 'password'
          secretRef: 'Smtp--Password'
        }
        {
          name: 'skipTLSVerify'
          value: 'true'
        }
      ]
      scopes: [
        'finecollectionservice'
      ]
    }
  }
}
