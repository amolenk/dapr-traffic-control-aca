// create web-app
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISpeedingViolationCalculator>(
    new DefaultSpeedingViolationCalculator("A12", 10, 100, 5));

builder.Services.AddSingleton<IVehicleStateRepository, DaprVehicleStateRepository>();

builder.Services.AddDaprClient();

builder.Services.AddControllers();

builder.Services.AddActors(options =>
{
    options.Actors.RegisterActor<VehicleActor>();
});

var app = builder.Build();

// configure web-app
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseCloudEvents();

// configure routing
app.MapControllers();
app.MapActorsHandlers();

// let's go!
app.Run();
