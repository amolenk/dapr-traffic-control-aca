$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:URLS = "http://*:6002"
$Env:DAPR_HTTP_PORT = 3602
$Env:DAPR_GRPC_PORT = 60002

dapr run `
    --app-id vehicleregistrationservice `
    --app-port 6002 `
    --dapr-http-port 3602 `
    --dapr-grpc-port 60002 `
    --config ../dapr/configuration/config.yaml `
    --components-path ../dapr/components `
    dotnet run
