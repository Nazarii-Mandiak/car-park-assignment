using CarPark.Api.Contracts;
using CarPark.Api.Data;
using CarPark.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CarPark.Tests;

public class ParkingServiceTests
{
    private sealed class TestFactory(DbContextOptions<ParkingDbContext> options) : IDbContextFactory<ParkingDbContext>
    {
        public ParkingDbContext CreateDbContext() => new(options);
    }

    private sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Current { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Current;
    }

    private static (ParkingService Service, ManualClock Clock) CreateService(int capacity = 10)
    {
        var options = new DbContextOptionsBuilder<ParkingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var clock = new ManualClock(new DateTimeOffset(2026, 11, 17, 12, 0, 0, TimeSpan.Zero));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Parking:Capacity"] = capacity.ToString() }).Build();
        return (new ParkingService(new TestFactory(options), clock, config), clock);
    }

    [Fact]
    public void ParksInLowestAvailableSpace_AndReusesReleasedSpace()
    {
        var (service, _) = CreateService();
        Assert.Equal(1, service.Park(new ParkRequest("ab12", 1)).Result!.SpaceNumber);
        Assert.Equal(2, service.Park(new ParkRequest("cd34", 2)).Result!.SpaceNumber);
        Assert.Equal(2, service.GetAvailability().OccupiedSpaces);
        Assert.Equal(8, service.GetAvailability().AvailableSpaces);
        Assert.Equal(200, service.Exit(new ExitRequest("ab12")).Status);
        Assert.Equal(1, service.Park(new ParkRequest("ef56", 3)).Result!.SpaceNumber);
    }

    [Fact]
    public void RejectsInvalidVehicleTypeAndDuplicateRegistration()
    {
        var (service, _) = CreateService();
        Assert.Equal(400, service.Park(new ParkRequest("AB12", 4)).Status);
        Assert.Equal(201, service.Park(new ParkRequest("AB12", 1)).Status);
        Assert.Equal(409, service.Park(new ParkRequest("ab12", 2)).Status);
    }

    [Fact]
    public void RejectsParkingWhenFull()
    {
        var (service, _) = CreateService(1);
        Assert.Equal(201, service.Park(new ParkRequest("AB12", 1)).Status);
        Assert.Equal(409, service.Park(new ParkRequest("CD34", 1)).Status);
        Assert.Equal(0, service.GetAvailability().AvailableSpaces);
    }

    [Theory]
    [InlineData(1, 12, 3.20)]
    [InlineData(2, 12, 4.40)]
    [InlineData(3, 12, 6.80)]
    [InlineData(2, 5, 2.00)]
    [InlineData(1, 0, 0.00)]
    public void CalculatesCharges(int type, int minutes, double expected)
    {
        Assert.Equal((decimal)expected, ParkingService.CalculateCharge(type, TimeSpan.FromMinutes(minutes)));
    }

    [Fact]
    public void ExitCalculatesChargeAndFreesSpace()
    {
        var (service, clock) = CreateService(1);
        service.Park(new ParkRequest("AB12", 2));
        clock.Current = clock.Current.AddMinutes(12);
        var exit = service.Exit(new ExitRequest("AB12"));
        Assert.Equal(200, exit.Status);
        Assert.Equal(4.40, exit.Result!.VehicleCharge, 2);
        Assert.Equal(1, service.GetAvailability().AvailableSpaces);
        Assert.Equal(404, service.Exit(new ExitRequest("AB12")).Status);
    }
}
