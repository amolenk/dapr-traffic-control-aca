$applicationDisplayName = 'DaprTrafficControlUI'

# OR.. don't check and patch...
# $applicationClientId = (az ad app list --display-name $applicationDisplayName --query '[0].appId' -o tsv)
# $applicationExists = $applicationClientId.Length -gt 0
# if (!$applicationExists) {
#     az ad app create --display-name 'DaprTrafficControlUI'
# }

# Create application (or patch if it already exists).
$applicationClientId = (az ad app create --display-name $applicationDisplayName | ConvertFrom-JSON).appId

$applicationUrl = 'https://trafficcontrolui.wittysand-fa5a5360.westeurope.azurecontainerapps.io'

# Get client secret
$clientSecret = (az ad app credential reset `
    --id $applicationClientId `
    --display-name 'Generated for Azure Container App' | ConvertFrom-Json).password

# Update app registration for EasyAuth.
az ad app update `
    --id $applicationClientId `
    --web-redirect-uris "$applicationUrl/.auth/login/aad/callback" `
    --enable-id-token-issuance true `
    --web-home-page-url $applicationUrl `
    --identifier-uris "api://$applicationClientId"

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

az deployment group create `
    --resource-group DaprTrafficControlACA `
    --template-file main.bicep `
    --parameters sqlAdministratorLoginPassword=Pass@word authClientId=$applicationClientId authClientSecret=$clientSecret

