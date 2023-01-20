$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:URLS = "http://*:6004"
$Env:DAPR_HTTP_PORT = 3604
$Env:DAPR_GRPC_PORT = 60004

dapr run `
    --app-id simulationgateway `
    --app-port 6004 `
    --dapr-http-port 3604 `
    --dapr-grpc-port 60004 `
    --config ../dapr/configuration/config.yaml `
    --components-path ../dapr/components `
    dotnet run
