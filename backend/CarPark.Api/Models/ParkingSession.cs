namespace CarPark.Api.Models;

public sealed class ParkingSession
{
    public int Id { get; set; }
    public required string VehicleReg { get; set; }
    public int VehicleType { get; set; }
    public int SpaceNumber { get; set; }
    public DateTime TimeIn { get; set; }
    public DateTime? TimeOut { get; set; }
}
