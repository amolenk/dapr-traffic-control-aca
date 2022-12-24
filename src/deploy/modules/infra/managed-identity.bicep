param location string
param uniqueSeed string

param managedIdentityName string = 'identity-${uniqueString(uniqueSeed)}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: managedIdentityName
  location: location
}

output managedIdentityName string = managedIdentity.name
