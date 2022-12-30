$Env:ASPNETCORE_ENVIRONMENT = "Development"
$Env:URLS = "http://*:6001"
$Env:DAPR_HTTP_PORT = 3601
$Env:DAPR_GRPC_PORT = 60001

dapr run `
    --app-id finecollectionservice `
    --app-port 6001 `
    --dapr-http-port 3601 `
    --dapr-grpc-port 60001 `
    --config ../dapr/configuration/config.yaml `
    --components-path ../dapr/components `
    dotnet run
