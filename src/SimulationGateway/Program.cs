// create web-app
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(builder =>
{
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
});

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
