namespace FineCollectionService.Events;

public class FineCalculated
{
    public string Id { get; private set; }
    public decimal Amount { get; private set; }
    public string VehicleId { get; private set; }
    public string RoadId { get; private set; }
    public string VehicleOwnerName { get; private set; }
    public string VehicleOwnerEmail { get; private set; }
    public string VehicleBrand { get; private set; }
    public string VehicleModel { get; private set; }
    public decimal ViolationInKmh { get; private set; }
    public DateTime Timestamp { get; private set; }

    public FineCalculated(
        string id,
        decimal amount,
        string vehicleId,
        string roadId,
        string vehicleOwnerName,
        string vehicleOwnerEmail,
        string vehicleBrand,
        string vehicleModel,
        decimal violationInKmh,
        DateTime timestamp)
    {
        Id = id;
        Amount = amount;
        VehicleId = vehicleId;
        RoadId = roadId;
        VehicleOwnerName = vehicleOwnerName;
        VehicleOwnerEmail = vehicleOwnerEmail;
        VehicleBrand = vehicleBrand;
        VehicleModel = vehicleModel;
        ViolationInKmh = violationInKmh;
        Timestamp = timestamp;        
    }
}
