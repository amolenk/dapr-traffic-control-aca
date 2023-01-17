$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:URLS = "http://*:6003"
$Env:DAPR_HTTP_PORT = 3603
$Env:DAPR_GRPC_PORT = 60003

dapr run `
    --app-id trafficcontrolui `
    --app-port 6003 `
    --dapr-http-port 3603 `
    --dapr-grpc-port 60003 `
    --config ../dapr/configuration/config.yaml `
    --components-path ../dapr/components `
    dotnet run
