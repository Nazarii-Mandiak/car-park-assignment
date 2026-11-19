using System.Text.Json;
using CarPark.Api.Contracts;
using CarPark.Api.Data;
using CarPark.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Exact casing as required: VehicleReg, SpaceNumber, TimeIn, etc.
    options.SerializerOptions.PropertyNamingPolicy = null;
});
builder.Services.AddDbContextFactory<ParkingDbContext>(options => options.UseInMemoryDatabase("CarPark"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ParkingService>();
builder.Services.AddCors(options => options.AddPolicy("AngularDev", policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AngularDev");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/parking", (ParkRequest? request, ParkingService service) =>
{
    var (result, error, status) = service.Park(request);
    return result is not null
        ? Results.Json(result, statusCode: status)
        : Results.Json(new ErrorResponse(error!), statusCode: status);
});

app.MapGet("/parking", (ParkingService service) => Results.Ok(service.GetAvailability()));

app.MapPost("/parking/exit", (ExitRequest? request, ParkingService service) =>
{
    var (result, error, status) = service.Exit(request);
    return result is not null
        ? Results.Json(result, statusCode: status)
        : Results.Json(new ErrorResponse(error!), statusCode: status);
});

app.Run();

public partial class Program { }
