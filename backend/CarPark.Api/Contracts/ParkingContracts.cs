namespace CarPark.Api.Contracts;

// PascalCase members intentionally match the assessment's exact API contract.
public sealed record ParkRequest(string? VehicleReg, int VehicleType);
public sealed record ParkResponse(string VehicleReg, int SpaceNumber, DateTime TimeIn);
public sealed record AvailabilityResponse(int AvailableSpaces, int OccupiedSpaces);
public sealed record ExitRequest(string? VehicleReg);
public sealed record ExitResponse(string VehicleReg, double VehicleCharge, DateTime TimeIn, DateTime TimeOut);
public sealed record ErrorResponse(string Error);
