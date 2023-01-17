namespace TrafficControlService.Events;

public record struct SpeedingViolationDetected(
    string Id,
    string VehicleId,
    string RoadId,
    int ViolationInKmh,
    DateTime Timestamp);