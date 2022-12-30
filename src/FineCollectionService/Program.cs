// create web-app
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddSingleton<IFineCalculator, HardCodedFineCalculator>()
    .AddTransient<SpeedingViolationHandler>()
    .AddTransient<QueryRecentFinesHandler>();

builder.Services.AddDbContext<FineDbContext>(
    options => options.UseSqlServer(builder.Configuration["ConnectionStrings:FineDb"]));

builder.Services.AddDaprClient();

builder.Configuration.AddDaprSecretStore(
    "secretstore",
    new DaprClientBuilder().Build());

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

// Configure routes

app.MapPost("/speedingviolation", async (
    [FromBody] SpeedingViolation speedingViolation,
    [FromServices] SpeedingViolationHandler handler) =>
{
    await handler.HandleAsync(speedingViolation);
    return Results.Ok();
}).WithTopic("pubsub", "speedingviolations");

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
