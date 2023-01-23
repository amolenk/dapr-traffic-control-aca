######
# Infrastructure
######

$resourceGroupName = 'DaprTrafficControlACA'

# Create resource group
az group create --name $resourceGroupName --location WestEurope

# Deploy infra resources
$outputs = (az deployment group create `
    --resource-group $resourceGroupName `
    --template-file main.bicep `
    --parameters deployApps=false sqlAdministratorLoginPassword=Pass@word `
    | ConvertFrom-Json).properties.outputs

$containerAppsEnvironmentDomain = $outputs.containerAppsEnvironmentDomain.value

az cosmosdb sql role assignment create `
    --account-name $outputs.cosmosAccountName.value `
    --resource-group $resourceGroupName `
    --role-definition-name "Cosmos DB Built-in Data Contributor" `
    --scope "/dbs/$($outputs.cosmosDbName.value)/colls/$($outputs.cosmosCollectionName.value)" `
    --principal-id $outputs.managedIdentityPrincipalId.value

######
# Application registrations
######

$applicationDisplayName = 'DaprTrafficControlUI'

# Create application (or patch if it already exists).
$applicationClientId = (az ad app create --display-name $applicationDisplayName | ConvertFrom-JSON).appId

$applicationUrl = "https://trafficcontrolui.$containerAppsEnvironmentDomain"

# Update app registration for EasyAuth.
az ad app update `
    --id $applicationClientId `
    --web-redirect-uris "$applicationUrl/.auth/login/aad/callback" `
    --enable-id-token-issuance true `
    --web-home-page-url $applicationUrl `
    --identifier-uris "api://$applicationClientId"

# Get client secret
$clientSecret = (az ad app credential reset `
    --id $applicationClientId `
    --display-name 'Generated for Azure Container App' | ConvertFrom-Json).password

# Add user-impersonation scope to app registration.

$applicationObjectId = (az ad app show --id $applicationClientId | ConvertFrom-JSON).id

$scopeBody =@{
    'api' = @{
        'oauth2PermissionScopes' = @(
            @{
                adminConsentDescription = 'Access to Dapr Traffic Control'
                adminConsentDisplayName = 'Allow the application to access Dapr Traffic Control on behalf of the signed-in user.' 
                id = [guid]::NewGuid()
                isEnabled = $true
                type = 'User'
                userConsentDescription = 'Access to Dapr Traffic Control'
                userConsentDisplayName = 'Allow the application to access Dapr Traffic Control on your behalf.'
                value = 'user-impersonation'
            }
        )
    }
} | ConvertTo-Json -d 4

az rest --method PATCH `
    --uri "https://graph.microsoft.com/v1.0/applications/$applicationObjectId" `
    --body $scopeBody

######
# Container Apps
######

# Deploy app resources
az deployment group create `
    --resource-group $resourceGroupName `
    --template-file main.bicep `
    --parameters deployInfra=false sqlAdministratorLoginPassword=Pass@word authClientId=$applicationClientId authClientSecret=$clientSecret

Write-Host "Auth Client ID = $applicationClientId"
Write-Host "Auth Client Secret = $clientSecret"
Write-Host "Done!"









