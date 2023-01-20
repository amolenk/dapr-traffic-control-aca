// create web-app
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

var app = builder.Build();

// Configure routes

app.MapPost("/api/entry", async (VehicleRegistered msg, DaprClient daprClient) =>
{
    // Forward to Mosquitto using MQTT output binding.
    await daprClient.InvokeBindingAsync("entrycam", "create", msg);

    return Results.Ok();
});

app.MapPost("/api/exit", async (VehicleRegistered msg, DaprClient daprClient) =>
{
    // Forward to Mosquitto using MQTT output binding.
    await daprClient.InvokeBindingAsync("exitcam", "create", msg);

    return Results.Ok();
});

// let's go!
app.Run();
