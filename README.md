# Car Park Management — ASP.NET Core + Angular

A simple car park management application with a .NET 8 REST API, Angular frontend and xUnit tests.

## Requirements

- .NET SDK 8.x
- Node.js 20.11+ or 22.x and npm

## Run the backend

```bash
cd backend/CarPark.Api
dotnet restore
dotnet run --launch-profile http
```

The API listens at `http://localhost:5080` and Swagger is available in Development at `http://localhost:5080/swagger`.

## Run the frontend

In a second terminal:

```bash
cd frontend
npm install
npm start
```

Visit `http://localhost:4200`. The frontend points to the API at `http://localhost:5080`.

## Run unit tests

```bash
cd backend
dotnet test CarPark.Tests/CarPark.Tests.csproj
```

## Database / initialization

Entity Framework Core's InMemory provider is used; **no external database, migration or SQL seed script is necessary**. The database is created automatically at application startup when first accessed. All spaces are initially available because spaces are represented as numbers `1..Parking:Capacity` (default: 10), rather than rows to seed. Parking sessions are stored in memory and **will be lost when the API restarts**. To change the number of spaces, update `Parking:Capacity` in `backend/CarPark.Api/appsettings.json` and restart the API.

## Required API contract

### POST `/parking`

Request:

```json
{ "VehicleReg": "AB12 CDE", "VehicleType": 2 }
```

Response (HTTP 201):

```json
{ "VehicleReg": "AB12 CDE", "SpaceNumber": 1, "TimeIn": "2026-11-17T12:00:00Z" }
```

### GET `/parking`

Response (HTTP 200):

```json
{ "AvailableSpaces": 9, "OccupiedSpaces": 1 }
```

### POST `/parking/exit`

Request:

```json
{ "VehicleReg": "AB12 CDE" }
```

Response (HTTP 200):

```json
{ "VehicleReg": "AB12 CDE", "VehicleCharge": 4.4, "TimeIn": "2026-11-17T12:00:00Z", "TimeOut": "2026-11-17T12:12:00Z" }
```

## Validation / error responses

- HTTP 400: missing/empty registration or vehicle type outside `1..3`.
- HTTP 409: vehicle already parked or car park full.
- HTTP 404: trying to exit a vehicle that is not parked.

Errors have the shape `{ "Error": "description" }`.

## Assumptions and trade-offs

1. **Capacity:** 10 parking spaces by default, configurable in `appsettings.json`.
2. **Allocation:** Spaces are numbered from 1. A new vehicle gets the lowest-numbered free space.
3. **Registration:** Leading/trailing whitespace is removed and registrations are stored uppercase for case-insensitive comparisons. Internal spaces are preserved.
4. **Time:** UTC is recorded for entry and exit. Angular displays dates in the browser's local timezone.
5. **Billing:** Every **started** minute is charged at the vehicle's rate. An additional £1 is charged for every **completed** five-minute period, measured using actual elapsed time. A stay shorter than a minute is billed as one minute (except zero elapsed time, which costs £0). Example: a 12-minute medium car stay costs `12 * £0.20 + 2 * £1 = £4.40`.
6. **Money:** `decimal` is used for calculations to avoid floating-point arithmetic errors; response `VehicleCharge` is `double` to match the required contract.
7. **Persistence:** EF Core InMemory is suitable for the assignment but is not durable and is not suitable for multi-instance deployments.
8. **Concurrency:** A lock around parking operations avoids double allocation within this single server instance. A production database implementation would use transactions and database constraints.
9. **Scope:** The Angular interface implements the same three operations as the backend. It deliberately avoids extra endpoints not requested by the assessment.

## Potential clarifications for the product owner

- How many spaces are in the car park, and should capacity be configurable by an administrator?
- Should partial minutes be rounded up or down?
- Is the £1 surcharge due after each completed five-minute period or at the beginning of each block?
- Should parking sessions survive server restarts?
