$appName=$args[0]

$revisionName = (az containerapp revision list `
    --name $appName `
    --resource-group DaprTrafficControlACA `
    | ConvertFrom-Json)[0].name

az containerapp revision restart `
    --name $appName `
    --resource-group DaprTrafficControlACA `
    --revision $revisionName
