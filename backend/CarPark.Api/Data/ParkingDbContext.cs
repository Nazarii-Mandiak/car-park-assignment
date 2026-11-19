using CarPark.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Data;

public sealed class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
    public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
}
