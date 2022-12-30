// create web-app
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IVehicleInfoRepository, InMemoryVehicleInfoRepository>();

builder.Services.AddDaprClient();

builder.Services.AddControllers().AddDapr();

var app = builder.Build();

// configure web-app
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseCloudEvents();

// configure routing
app.MapControllers();

// let's go!
app.Run();
