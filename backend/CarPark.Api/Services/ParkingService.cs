using CarPark.Api.Contracts;
using CarPark.Api.Data;
using CarPark.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Services;

public sealed class ParkingService
{
    private readonly IDbContextFactory<ParkingDbContext> _dbFactory;
    private readonly TimeProvider _clock;
    private readonly int _capacity;
    // Serializes allocations/exits to prevent two vehicles taking the same space.
    private readonly object _gate = new();

    public ParkingService(IDbContextFactory<ParkingDbContext> dbFactory, TimeProvider clock, IConfiguration config)
    {
        _dbFactory = dbFactory;
        _clock = clock;
        _capacity = config.GetValue("Parking:Capacity", 10);
        if (_capacity <= 0) throw new ArgumentException("Parking:Capacity must be positive.");
    }

    public (ParkResponse? Result, string? Error, int Status) Park(ParkRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.VehicleReg))
            return (null, "VehicleReg is required.", 400);
        if (request.VehicleType is < 1 or > 3)
            return (null, "VehicleType must be 1, 2 or 3.", 400);
        string reg = request.VehicleReg.Trim().ToUpperInvariant();

        lock (_gate)
        {
            using var db = _dbFactory.CreateDbContext();
            var active = db.ParkingSessions.Where(s => s.TimeOut == null).ToList();
            if (active.Any(s => s.VehicleReg == reg))
                return (null, "Vehicle is already parked.", 409);

            var occupied = active.Select(s => s.SpaceNumber).ToHashSet();
            int space = Enumerable.Range(1, _capacity).FirstOrDefault(n => !occupied.Contains(n));
            if (space == 0) return (null, "Parking lot is full.", 409);

            var timeIn = _clock.GetUtcNow().UtcDateTime;
            db.ParkingSessions.Add(new ParkingSession
            {
                VehicleReg = reg, VehicleType = request.VehicleType,
                SpaceNumber = space, TimeIn = timeIn
            });
            db.SaveChanges();
            return (new ParkResponse(reg, space, timeIn), null, 201);
        }
    }

    public AvailabilityResponse GetAvailability()
    {
        lock (_gate)
        {
            using var db = _dbFactory.CreateDbContext();
            int occupied = db.ParkingSessions.Count(s => s.TimeOut == null);
            return new AvailabilityResponse(_capacity - occupied, occupied);
        }
    }

    public (ExitResponse? Result, string? Error, int Status) Exit(ExitRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.VehicleReg))
            return (null, "VehicleReg is required.", 400);
        string reg = request.VehicleReg.Trim().ToUpperInvariant();

        lock (_gate)
        {
            using var db = _dbFactory.CreateDbContext();
            var session = db.ParkingSessions.FirstOrDefault(s => s.VehicleReg == reg && s.TimeOut == null);
            if (session is null) return (null, "Vehicle is not currently parked.", 404);

            var timeOut = _clock.GetUtcNow().UtcDateTime;
            var charge = CalculateCharge(session.VehicleType, timeOut - session.TimeIn);
            session.TimeOut = timeOut;
            db.SaveChanges();
            return (new ExitResponse(reg, (double)charge, session.TimeIn, timeOut), null, 200);
        }
    }

    public static decimal CalculateCharge(int vehicleType, TimeSpan duration)
    {
        if (vehicleType is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(vehicleType));
        // Every started minute is billed; £1 extra for every *completed* 5-minute block.
        int minutes = (int)Math.Ceiling(Math.Max(0, duration.TotalMinutes));
        decimal rate = vehicleType switch { 1 => 0.10m, 2 => 0.20m, _ => 0.40m };
        int completedFiveMinuteBlocks = (int)Math.Floor(Math.Max(0, duration.TotalMinutes) / 5);
        return Math.Round(minutes * rate + completedFiveMinuteBlocks, 2, MidpointRounding.AwayFromZero);
    }
}
