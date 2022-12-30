$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:URLS = "http://*:6000"
$Env:DAPR_HTTP_PORT = 3600
$Env:DAPR_GRPC_PORT = 60000

dapr run `
    --app-id trafficcontrolservice `
    --app-port 6000 `
    --dapr-http-port 3600 `
    --dapr-grpc-port 60000 `
    --config ../dapr/configuration/config.yaml `
    --components-path ../dapr/components `
    dotnet run
