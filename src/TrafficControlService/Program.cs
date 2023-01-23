// create web-app
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();

builder.Services.AddSingleton<ISpeedingViolationCalculator>(
    new DefaultSpeedingViolationCalculator("A12", 10, 100, 5));

var app = builder.Build();

// Endpoints
app.MapPost("/entrycam", async (VehicleRegistered msg, DaprClient daprClient, ILogger<Program> logger) =>
{
    // log entry
    logger.LogInformation($"ENTRY detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} " +
        $"of vehicle with license-number {msg.LicenseNumber}.");

    // store vehicle state
    var vehicleState = new VehicleState(msg.LicenseNumber, msg.Timestamp);

    await daprClient.SaveStateAsync(
        "statestore",
        vehicleState.LicenseNumber,
        vehicleState);

    return Results.Ok();
});

app.MapPost("/exitcam", async (VehicleRegistered msg, ISpeedingViolationCalculator calculator, DaprClient daprClient, ILogger<Program> logger) =>
{
    logger.LogInformation($"EXIT detected in lane {msg.Lane} at {msg.Timestamp.ToString("hh:mm:ss")} " +
        $"of vehicle with license-number {msg.LicenseNumber}.");

    var state = await daprClient.GetStateEntryAsync<VehicleState>(
        "statestore",
        msg.LicenseNumber);

    if (state is null)
    {
        logger.LogError($"Entry timestamp not found for vehicle {msg.LicenseNumber}.");
        return Results.NotFound();
    }

    // update state
    if (!state.Value.ExitTimestamp.HasValue)
    {
        state.Value = state.Value with { ExitTimestamp = msg.Timestamp };
        await state.SaveAsync();
    }

    // handle possible speeding violation
    int violation = calculator.DetermineSpeedingViolationInKmh(
        state.Value.EntryTimestamp,
        state.Value.ExitTimestamp.Value);
    
    if (violation > 0)
    {
        logger.LogInformation($"Speeding violation detected ({violation} KMh) for vehicle" +
            $" with license-number {state.Value.LicenseNumber}.");

        var SpeedingViolationDetected = new SpeedingViolationDetected
        {
            Id = $"{msg.LicenseNumber}@{state.Value.ExitTimestamp.Value:s}",
            VehicleId = msg.LicenseNumber,
            RoadId = calculator.GetRoadId(),
            ViolationInKmh = violation,
            Timestamp = msg.Timestamp
        };

        // publish speedingviolation
        await daprClient.PublishEventAsync(
            "pubsub",
            "speedingviolations",
            SpeedingViolationDetected);
    }

    return Results.Ok();
});

// let's go!
app.Run();
