namespace TrafficControlService.Models;

public record struct SpeedingViolation(string Id, string VehicleId, string RoadId, int ViolationInKmh, DateTime Timestamp);