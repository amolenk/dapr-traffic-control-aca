// create web-app
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDaprClient();

builder.Configuration.AddDaprSecretStore(
    "secretstore",
    new DaprClientBuilder().Build(),
    new string[] { "--" });

Console.WriteLine("YO!!");

Console.WriteLine(builder.Configuration["Smtp:Password"]);
Console.WriteLine(builder.Configuration["ConnectionStrings:FineDb"]);

builder.Services
    .AddSingleton<IFineCalculator, HardCodedFineCalculator>()
    .AddTransient<SpeedingViolationHandler>()
    .AddTransient<QueryRecentFinesHandler>();

builder.Services.AddDbContext<FineDbContext>(
    options => options.UseSqlServer(builder.Configuration["ConnectionStrings:FineDb"]));

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

var httpClient = DaprClient.CreateInvokeHttpClient();

// Configure routes

app.MapPost("/speedingviolation", async (
    [FromBody] SpeedingViolation speedingViolation,
    [FromServices] SpeedingViolationHandler handler) =>
{
    await handler.HandleAsync(speedingViolation);
    return Results.Ok();
})
.WithTopic("pubsub", "speedingviolations");

app.MapPost("/speedingviolation", async (
    SpeedingViolation speedingViolation,
    IFineCalculator fineCalculator,
    DaprClient daprClient,
    IConfiguration configuration,
    ILogger logger) =>
{
    var licenseKey = configuration["FineCalculator:LicenseKey"];

    var fineAmount = fineCalculator.CalculateFine(
        licenseKey,
        speedingViolation.ViolationInKmh);

    var vehicleInfo = await httpClient.GetFromJsonAsync<VehicleInfo>(
        $"http://vehicleregistrationservice/vehicleinfo/{licenseNumber}");


    // log fine
    string fineString = fineAmount == 0 ? "tbd by the prosecutor" : $"{fineAmount} Euro";
    logger.LogInformation($"Sent speeding ticket to {vehicleInfo.OwnerName}. " +
        $"Road: {speedingViolation.RoadId}, Licensenumber: {speedingViolation.VehicleId}, " +
        $"Vehicle: {vehicleInfo.Brand} {vehicleInfo.Model}, " +
        $"Violation: {speedingViolation.ViolationInKmh} Km/h, Fine: {fineString}, " +
        $"On: {speedingViolation.Timestamp.ToString("dd-MM-yyyy")} " +
        $"at {speedingViolation.Timestamp.ToString("hh:mm:ss")}.");

//    await daprClient.PublishEventAsync("pubsub", "finecalculated", speedingViolation);
    return Results.Ok();
})
.WithTopic("pubsub", "speedingviolations");


app.MapGet("/query/recent", async ([FromServices] QueryRecentFinesHandler handler) =>
{
    var recentFines = await handler.HandleAsync();
    return Results.Ok(recentFines);
});

app.MapSubscribeHandler();

// Migrate database
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<FineDbContext>();
await dbContext.Database.MigrateAsync();

// let's go!
app.Run();
