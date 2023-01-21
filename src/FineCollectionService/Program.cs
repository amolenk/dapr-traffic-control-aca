// create web-app
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDaprClient();

builder.Configuration.AddDaprSecretStore(
    "secretstore",
    new DaprClientBuilder().Build(),
    new string[] { "--" });

builder.Services.AddSingleton<IFineCalculator, HardCodedFineCalculator>();

builder.Services.AddSingleton<VehicleRegistrationServiceClient>(_ =>
    new VehicleRegistrationServiceClient(DaprClient.CreateInvokeHttpClient(
        "vehicleregistrationservice")));

var app = builder.Build();

// configure web-app
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCloudEvents();
app.MapSubscribeHandler();

var httpClient = DaprClient.CreateInvokeHttpClient();

// Configure routes

app.MapPost("/speedingviolation", async (
    SpeedingViolationDetected msg,
    IFineCalculator fineCalculator,
    DaprClient daprClient,
    IConfiguration configuration,
    ILogger<Program> logger) =>
{
    var licenseKey = configuration["FineCalculator:LicenseKey"];

    var fineAmount = fineCalculator.CalculateFine(
        licenseKey,
        msg.ViolationInKmh);

    var vehicleInfo = await httpClient.GetFromJsonAsync<VehicleInfo>(
        $"http://vehicleregistrationservice/vehicleinfo/{msg.VehicleId}");

    var fineCalculated = new FineCalculated(
        msg.Id,
        fineAmount,
        msg.VehicleId,
        msg.RoadId,
        vehicleInfo.OwnerName,
        vehicleInfo.OwnerEmail,
        vehicleInfo.Brand,
        vehicleInfo.Model,
        msg.ViolationInKmh,
        msg.Timestamp);

    // log fine
    logger.LogInformation($"Calculated fine amount for speeding ticket {fineCalculated.Id}.");

    await daprClient.PublishEventAsync("pubsub", "fines", fineCalculated);

    return Results.Ok();
})
.WithTopic("pubsub", "speedingviolations");

app.MapPost("/finecalculated", async (FineCalculated msg, DaprClient daprClient) =>
{
    var body = EmailUtils.CreateEmailBody(msg);

    var metadata = new Dictionary<string, string>
    {
        ["emailFrom"] = "noreply@cfca.gov",
        ["emailTo"] = msg.VehicleOwnerEmail,
        ["subject"] = $"Speeding violation on {msg.RoadId}"
    };

    await daprClient.InvokeBindingAsync("sendmail", "create", body, metadata);

    return Results.Ok();
})
.WithTopic("pubsub", "fines");

// let's go!
app.Run();
