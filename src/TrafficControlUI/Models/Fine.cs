namespace TrafficControlUI.Models;

public class Fine
{
    public string Id { get; private set; }
    public decimal Amount { get; private set; }
    public string VehicleId { get; private set; }
    public string RoadId { get; private set; }
    public string VehicleBrand { get; private set; }
    public string VehicleModel { get; private set; }
    public decimal ViolationInKmh { get; private set; }
    public DateTime Timestamp { get; private set; }

    public Fine(
        string id,
        decimal amount,
        string vehicleId,
        string roadId,
        string vehicleBrand,
        string vehicleModel,
        decimal violationInKmh,
        DateTime timestamp)
    {
        Id = id;
        Amount = amount;
        VehicleId = vehicleId;
        RoadId = roadId;
        VehicleBrand = vehicleBrand;
        VehicleModel = vehicleModel;
        ViolationInKmh = violationInKmh;
        Timestamp = timestamp;        
    }
}
