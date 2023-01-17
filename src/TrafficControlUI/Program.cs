using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using TrafficControlUI;
using TrafficControlUI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDaprClient();

builder.Configuration.AddDaprSecretStore(
    "secretstore",
    new DaprClientBuilder().Build(),
    new string[] { "--" });

builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, EasyAuthStateProvider>();
builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddDbContext<FineDbContext>(
    options => options.UseSqlServer(builder.Configuration["ConnectionStrings:FineDb"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.UseCloudEvents();
app.MapSubscribeHandler();

// Configure routes

app.MapPost("/finecalculated", async (FineCalculated fineCalculated, ILogger<Program> logger) =>
{
    logger.LogWarning("Got a fine: " + fineCalculated.Id + " - " + fineCalculated.VehicleId);

    await Task.Yield();

    return Results.Ok();
})
.WithTopic("pubsub", "finecalculated");

// Migrate database
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<FineDbContext>();
await dbContext.Database.MigrateAsync();

// Let's go!
app.Run();
