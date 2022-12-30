namespace FineCollectionService.Handlers;

public class SpeedingViolationHandler
{
    private const string LicenseKeySecretName = "FineCalculator:LicenseKey";

    private readonly IFineCalculator _fineCalculator;
    private readonly VehicleRegistrationServiceClient _vehicleRegistrationServiceClient;
    private readonly FineDbContext _fineDbContext;
    private readonly DaprClient _daprClient;
    private readonly string _licenseKey;
    private readonly ILogger _logger;

    public SpeedingViolationHandler(
        IFineCalculator fineCalculator,
        VehicleRegistrationServiceClient vehicleRegistrationServiceClient,
        FineDbContext fineDbContext,
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<SpeedingViolationHandler> logger)
    {
        _fineCalculator = fineCalculator;
        _vehicleRegistrationServiceClient = vehicleRegistrationServiceClient;
        _fineDbContext = fineDbContext;
        _daprClient = daprClient;
        _licenseKey = configuration[LicenseKeySecretName];
        _logger = logger;
    }

    public async Task HandleAsync(SpeedingViolation speedingViolation)
    {
        var fineAmount = _fineCalculator.CalculateFine(
            _licenseKey,
            speedingViolation.ViolationInKmh);

        var vehicleInfo = await _vehicleRegistrationServiceClient.GetVehicleInfo(
            speedingViolation.VehicleId);

        // log fine
        string fineString = fineAmount == 0 ? "tbd by the prosecutor" : $"{fineAmount} Euro";
        _logger.LogInformation($"Sent speeding ticket to {vehicleInfo.OwnerName}. " +
            $"Road: {speedingViolation.RoadId}, Licensenumber: {speedingViolation.VehicleId}, " +
            $"Vehicle: {vehicleInfo.Brand} {vehicleInfo.Model}, " +
            $"Violation: {speedingViolation.ViolationInKmh} Km/h, Fine: {fineString}, " +
            $"On: {speedingViolation.Timestamp.ToString("dd-MM-yyyy")} " +
            $"at {speedingViolation.Timestamp.ToString("hh:mm:ss")}.");

        await StoreFineAsync(fineAmount, speedingViolation, vehicleInfo);
        await SendEmailToVehicleOwnerAsync(speedingViolation, vehicleInfo, fineString);
    }

    private async Task StoreFineAsync(
        decimal fineAmount,
        SpeedingViolation speedingViolation,
        VehicleInfo vehicleInfo)
    {
        var fine = new Fine(
            speedingViolation.Id,
            fineAmount,
            speedingViolation.VehicleId,
            speedingViolation.RoadId,
            vehicleInfo.Brand,
            vehicleInfo.Model,
            speedingViolation.ViolationInKmh,
            speedingViolation.Timestamp);

        _fineDbContext.Add(fine);

        try
        {
            await _fineDbContext.SaveChangesAsync();
        }
        catch (UniqueConstraintException)
        {
            // Fine already exists in DB, must be duplicate message.
        }
    }

    private async Task SendEmailToVehicleOwnerAsync(
        SpeedingViolation speedingViolation,
        VehicleInfo vehicleInfo,
        string fineString)
    {
        var body = EmailUtils.CreateEmailBody(speedingViolation, vehicleInfo, fineString);
        var metadata = new Dictionary<string, string>
        {
            ["emailFrom"] = "noreply@cfca.gov",
            ["emailTo"] = vehicleInfo.OwnerEmail,
            ["subject"] = $"Speeding violation on the {speedingViolation.RoadId}"
        };

        await _daprClient.InvokeBindingAsync("sendmail", "create", body, metadata);
    }
}
