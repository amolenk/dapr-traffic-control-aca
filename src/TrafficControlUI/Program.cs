var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDaprClient();

builder.Configuration.AddDaprSecretStore(
    "secretstore",
    new DaprClientBuilder().Build(),
    new string[] { "--" });

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<AuthenticationStateProvider, EasyAuthStateProvider>();

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

app.MapPost("/finecalculated", async (FineCalculated msg, FineDbContext dbContext, ILogger<Program> logger) =>
{
    var fine = new Fine(
        msg.Id,
        msg.Amount,
        msg.VehicleId,
        msg.RoadId,
        msg.VehicleBrand,
        msg.VehicleModel,
        msg.ViolationInKmh,
        msg.Timestamp);

    dbContext.Add(fine);

    try
    {
        await dbContext.SaveChangesAsync();
    }
    catch (UniqueConstraintException)
    {
        // Fine already exists in DB, must be duplicate message.
    }

    return Results.Ok();
})
.WithTopic("pubsub", "fines");

// Migrate database
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<FineDbContext>();
await dbContext.Database.MigrateAsync();

// Let's go!
app.Run();
