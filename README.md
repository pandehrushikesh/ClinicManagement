# Clinic Management System

A learning-first project that evolves a **Clean Architecture monolith** into a **microservice system**, one phase at a time.
Each phase introduces a new architectural concern. The pain of each transition is the lesson.

---

## Evolution Roadmap

| Phase | Tag | Theme | Status |
|---|---|---|---|
| 1 | `phase-1-monolith` | Clean Monolith — domain model, CQRS, EF Core, REST API | ✅ Complete |
| 2 | `phase-2-auth-service` | Auth Service Extracted — JWT issuance, protected endpoints | ✅ Complete |
| 3 | `phase-3-async-messaging` | Async Messaging — RabbitMQ + NotificationService via MassTransit | ✅ Complete |
| 4 | `phase-4-api-gateway` | API Gateway — YARP reverse proxy, single client entry point | ✅ Complete |
| 5 | `phase-5-performance-layer` | Performance Layer — Redis cache, Dapper reads, CQRS matured | ✅ Complete |
| 6 | `phase-6-containerization` | Containerization — Docker Compose, full local orchestration | ✅ Complete |
| 7 | `phase-7-react-ui` | React UI — Vite + React + TypeScript frontend via the Gateway | ✅ Complete |

---

## Phase 1 — Clean Monolith

> Tag: `phase-1-monolith`

### Goal
Build a single deployable unit with clean internal boundaries that will survive surgical extraction in later phases.
No shortcuts that trade today's convenience for tomorrow's pain.

---

### Solution Structure

```
ClinicManagement/
├── src/
│   ├── ClinicManagement.API/              # ASP.NET Core Web API — controllers, middleware, DI wiring
│   ├── ClinicManagement.Application/      # Use cases — commands, queries, DTOs, repository interfaces
│   ├── ClinicManagement.Domain/           # Entities, value objects, enums, domain exceptions
│   ├── ClinicManagement.Infrastructure/   # EF Core, SQL Server, repository implementations
│   └── ClinicManagement.Shared/           # Reserved for cross-cutting concerns (Phase 2+)
├── tests/
│   └── ClinicManagement.UnitTests/        # Domain unit tests (xunit + FluentAssertions)
├── docs/
│   └── decisions/                         # Architecture Decision Records
└── CLAUDE.md                              # Architectural guardrails — read before writing code
```

---

### Layer Dependency Rules

```
Domain          → depends on nothing
Application     → depends on Domain only
Infrastructure  → depends on Application + Domain
API             → depends on Application only
```

The API project **never** references Infrastructure directly — it only resolves implementations through DI.
This is what makes Phase 2 extraction non-destructive.

```
HTTP Request
     │
     ▼
[API Controller]          — thin: accept input, dispatch, return response
     │
     ▼
[Application Handler]     — one class per use case, depends on interfaces only
     │
     ▼
[Domain Entity]           — business rules live here, private setters, behavior methods
     │
     ▼
[Infrastructure Repo]     — EF Core implementation, never leaks IQueryable
     │
     ▼
[SQL Server]
```

---

### Domain Model

#### Patient
- Private setters — state changes through methods only
- `Create()` — validates name, date of birth (must be in past), email, phone
- `Deactivate()` / `Reactivate()` — guarded state transitions
- `UpdateContactInfo()` — replaces email and phone atomically

#### Doctor
- `Create()` — name and specialty required
- `Deactivate()` — guarded, throws if already inactive

#### Appointment — State Machine
```
            ┌─────────────┐
            │  Scheduled  │◄──────────────────┐
            └──────┬──────┘                   │ Reschedule
                   │ Confirm                  │ (resets to Scheduled)
                   ▼                          │
            ┌─────────────┐                   │
            │  Confirmed  │───────────────────┘
            └──────┬──────┘
                   │ Complete
                   ▼
            ┌─────────────┐
            │  Completed  │   (terminal)
            └─────────────┘

  Scheduled or Confirmed ──► Cancel (reason required) ──► Cancelled (terminal)
```

Rules enforced in the entity:
- Can only `Confirm` a `Scheduled` appointment
- Can only `Complete` a `Confirmed` appointment
- Cannot `Cancel` or `Reschedule` a `Completed` or `Cancelled` appointment
- `Reschedule` resets status back to `Scheduled` (requires re-confirmation)
- Cancellation reason is mandatory

#### Value Objects
| Value Object | Validation |
|---|---|
| `Email` | Format validated via regex, stored lowercase |
| `PhoneNumber` | Digit count 7–15, stored as-is |

Both use C# `record` types — equality by value, immutable.

---

### CQRS Structure (without MediatR)

Each use case is a separate class in its own folder. Handlers are registered directly in DI — MediatR is intentionally deferred to Phase 2.

```
Application/
├── Patients/
│   ├── Commands/
│   │   └── CreatePatient/
│   │       ├── CreatePatientCommand.cs
│   │       └── CreatePatientCommandHandler.cs
│   ├── Queries/
│   │   ├── GetPatientById/
│   │   └── GetPatients/
│   └── PatientMappingExtensions.cs
└── Appointments/
    ├── Commands/
    │   ├── ScheduleAppointment/     — validates patient/doctor active + doctor conflict check
    │   ├── ConfirmAppointment/
    │   ├── CancelAppointment/
    │   ├── CompleteAppointment/
    │   └── RescheduleAppointment/   — conflict check excludes self
    └── Queries/
        ├── GetAppointmentById/
        └── GetAppointments/         — filterable by patient, doctor, status
```

---

### API Endpoints

All responses follow this envelope:
```json
{ "success": true, "data": {}, "error": null }
```

#### Patients
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/patients` | Register a new patient |
| `GET` | `/api/patients/{id}` | Get patient by ID |
| `GET` | `/api/patients?afterId=0&pageSize=20` | List patients (keyset paginated) |

#### Doctors
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/doctors` | Register a new doctor |

#### Appointments
| Method | Route | Description |
|---|---|---|
| `POST` | `/api/appointments` | Schedule an appointment |
| `GET` | `/api/appointments/{id}` | Get appointment by ID |
| `GET` | `/api/appointments?afterId=0&pageSize=20&patientId=&doctorId=&status=` | List (keyset paginated, filterable) |
| `PUT` | `/api/appointments/{id}/confirm` | Confirm a scheduled appointment |
| `PUT` | `/api/appointments/{id}/complete` | Complete a confirmed appointment |
| `PUT` | `/api/appointments/{id}/cancel` | Cancel (body: `{ "reason": "..." }`) |
| `PUT` | `/api/appointments/{id}/reschedule` | Reschedule (body: `{ "newScheduledAt": "...", "newDurationMinutes": 30 }`) |

Pagination is **keyset** (not offset) on all list endpoints — pass the last seen `id` as `afterId`.

---

### Infrastructure Decisions

#### EF Core
- Every entity has a dedicated `IEntityTypeConfiguration<T>` — no Fluent API in `OnModelCreating`
- Value objects (`Email`, `PhoneNumber`) stored as columns via `HasConversion`
- `AppointmentStatus` stored as `int`

#### Indexes
| Table | Index |
|---|---|
| Patients | Unique on `Email`; non-unique on `IsActive`, `LastName` |
| Doctors | Non-unique on `Specialty`, `IsActive` |
| Appointments | Non-unique on `PatientId`, `DoctorId`, `Status`, `ScheduledAt`; composite on `(DoctorId, ScheduledAt, Status)` for conflict detection |

#### Doctor Conflict Detection
Before scheduling or rescheduling, the system checks for overlapping appointments:
```
existing.ScheduledAt < newEnd  AND  existing.ScheduledAt + duration > newStart
```
Cancelled appointments are excluded. Reschedule excludes the appointment being moved.

---

### Error Handling

Global middleware maps domain exceptions to HTTP status codes:

| Exception | HTTP Status |
|---|---|
| `DomainException` | `400 Bad Request` |
| `NotFoundException` | `404 Not Found` |
| Unhandled | `500 Internal Server Error` + correlation ID |

---

### Running Locally

**Prerequisites:** .NET 10 SDK, SQL Server

1. Clone the repo
2. Create `src/ClinicManagement.API/appsettings.Development.json` (git-ignored):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ClinicManagement;..."
  }
}
```
3. Apply migrations:
```bash
dotnet ef database update --project src/ClinicManagement.Infrastructure --startup-project src/ClinicManagement.API
```
4. Run the API:
```bash
dotnet run --project src/ClinicManagement.API
```
5. Open Scalar UI: `https://localhost:{port}/scalar/v1`

---

### Unit Tests

```bash
dotnet test tests/ClinicManagement.UnitTests
```

**62 tests, 0 failures.** Coverage targets the domain layer — the only layer with logic worth unit testing.

| Test Class | Count | What it covers |
|---|---|---|
| `AppointmentTests` | 20 | Every state transition + every guard clause |
| `PatientTests` | 10 | Create, Deactivate, Reactivate, UpdateContactInfo |
| `DoctorTests` | 5 | Create, Deactivate |
| `EmailTests` | 10 | Valid formats, empty/null, invalid format, equality |
| `PhoneNumberTests` | 7 | Valid formats, empty/null, invalid length |

---

### What Phase 1 Deliberately Avoids

- **No MediatR** — handlers are registered directly; adding MediatR in Phase 2 is wiring, not restructuring
- **No Auth** — all endpoints are open; JWT issuance is Phase 2's entire lesson
- **No Docker** — containerization is Phase 6; don't compound complexity during architecture learning
- **No lazy loading** — all navigations use explicit `.Include()` or separate queries

---

## Phase 2 — Auth Service Extracted

> Tag: `phase-2-auth-service` | Branch: `Phase2_AuthServiceExtracted`

### Goal
Extract authentication into a dedicated service with its own database. The main API stops owning identity — it only validates tokens it did not issue. This is the first real service boundary.

---

### What Changed

#### New Service: `ClinicManagement.AuthService`
A standalone ASP.NET Core Web API. Completely independent — its own process, its own database, its own migrations.

```
ClinicManagement.AuthService/
├── Controllers/
│   └── AuthController.cs       # POST /auth/register, POST /auth/login
├── Entities/
│   └── User.cs                 # Email, PasswordHash (BCrypt), Role, VerifyPassword()
├── Persistence/
│   ├── AuthDbContext.cs        # Own EF Core context → ClinicManagement_Auth DB
│   └── AuthDbContextFactory.cs # Design-time factory for EF CLI migrations
├── Services/
│   └── TokenService.cs         # Issues HS256 JWT with sub/email/role/jti claims
└── Program.cs
```

#### New Project: `ClinicManagement.Shared`
Holds `JwtSettings` (Secret, Issuer, Audience, ExpiryMinutes) — the only thing both services need to agree on. Both services bind this from their own `appsettings.json`. No other shared code.

#### Updated: `ClinicManagement.API`
- Added `AddAuthentication` + `AddJwtBearer` — validates tokens, never issues them
- `[Authorize]` added to `PatientsController`, `DoctorsController`, `AppointmentsController`
- Zero changes to Application, Domain, or Infrastructure — the seams held

---

### Two Databases

| Database | Owns |
|---|---|
| `ClinicManagement` | Patients, Doctors, Appointments |
| `ClinicManagement_Auth` | Users (identity only) |

No foreign keys cross the database boundary. The main API has no knowledge of the `Users` table.

---

### Auth Flow

```
Client
  │
  ├─ POST /auth/register  ──► AuthService ──► hash password (BCrypt)
  │                                        ──► save User to ClinicManagement_Auth
  │                                        ──► return JWT
  │
  ├─ POST /auth/login     ──► AuthService ──► verify password
  │                                        ──► return JWT
  │
  └─ GET /api/patients    ──► ClinicManagement.API
       Authorization: Bearer <token>        ──► validate JWT signature + expiry
                                            ──► no call to AuthService needed
                                            ──► serve request
```

JWT validation is **stateless** — the main API verifies the token signature locally using the shared secret. No HTTP call to AuthService per request.

---

### JWT Token Contents

| Claim | Value |
|---|---|
| `sub` | User ID |
| `email` | User email |
| `role` | User role (`Staff`, `Admin`, etc.) |
| `jti` | Unique token ID (for future revocation) |
| `exp` | Expiry (configurable, default 60 min) |

---

### Auth Endpoints

| Method | Route | Auth required | Description |
|---|---|---|---|
| `POST` | `/auth/register` | No | Create account, returns JWT |
| `POST` | `/auth/login` | No | Verify credentials, returns JWT |

All existing `ClinicManagement.API` endpoints now require `Authorization: Bearer <token>`.

---

### Running Locally (Phase 2)

Run **both** services simultaneously (different ports):

```bash
dotnet run --project src/ClinicManagement.AuthService   # e.g. https://localhost:7001
dotnet run --project src/ClinicManagement.API           # e.g. https://localhost:7000
```

Create `src/ClinicManagement.AuthService/appsettings.Development.json` (git-ignored):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ClinicManagement_Auth;..."
  },
  "JwtSettings": {
    "Secret": "your-secret-min-32-chars",
    "Issuer": "ClinicManagement.AuthService",
    "Audience": "ClinicManagement.API",
    "ExpiryMinutes": 60
  }
}
```

Add the same `JwtSettings` block to `src/ClinicManagement.API/appsettings.Development.json` (secret must match).

Apply AuthService migration:
```bash
dotnet ef database update --project src/ClinicManagement.AuthService --startup-project src/ClinicManagement.AuthService
```

---

### The Phase 2 Lesson

**What the seams bought us:** Adding a full auth service required zero changes to Application, Domain, or Infrastructure. Only `Program.cs` and three controller attributes changed in the main API. That is Clean Architecture working as intended.

**What's new and painful:** Two processes to run locally. Two `appsettings.Development.json` files to maintain. The shared JWT secret is a deployment coupling — if you rotate it, both services must redeploy simultaneously. This pain is intentional; Phase 4's API Gateway will centralise some of it.

---

---

## Phase 3 — Async Messaging

> Tag: `phase-3-async-messaging` | Branch: `Phase3_AsyncMessaging`

### Goal
Decouple appointment scheduling from notification delivery. When an appointment is booked, the main API no longer cares who needs to be notified or how. It drops an event on a queue and moves on. The NotificationService picks it up independently — at its own pace, with its own retry logic.

---

### What Changed

#### New Service: `ClinicManagement.NotificationService`
A standalone ASP.NET Core app. No REST controllers — it exists purely to consume messages.

```
ClinicManagement.NotificationService/
├── Consumers/
│   └── AppointmentScheduledConsumer.cs   # IConsumer<AppointmentScheduledEvent>
└── Program.cs                            # MassTransit wired, no HTTP endpoints needed
```

#### Updated: `ClinicManagement.Shared`
Added `Events/AppointmentScheduledEvent.cs` — the shared message contract. Both the publisher (API) and consumer (NotificationService) reference this same record. This is the only coupling between the two services.

#### Updated: `ClinicManagement.Application`
`IEventPublisher` interface added to `Common/Interfaces/`. The `ScheduleAppointmentCommandHandler` depends on this interface — it knows nothing about RabbitMQ or MassTransit.

#### Updated: `ClinicManagement.Infrastructure`
`MassTransitEventPublisher` implements `IEventPublisher` using MassTransit's `IPublishEndpoint`. This is the only place MassTransit leaks into the main service's codebase.

#### Updated: `ClinicManagement.API`
MassTransit registered as publisher-only (no consumers). `IEventPublisher` registered as `MassTransitEventPublisher`.

---

### Async Flow

```
Client
  │
  └─ POST /api/appointments ──► ClinicManagement.API
                                  │
                                  ├─ validate JWT
                                  ├─ run ScheduleAppointmentCommandHandler
                                  ├─ save to SQL Server
                                  ├─ publish AppointmentScheduledEvent ──► RabbitMQ
                                  └─ return 201 (does NOT wait for notification)

                                                    RabbitMQ
                                                       │
                                  ClinicManagement.NotificationService
                                    └─ AppointmentScheduledConsumer.Consume()
                                        └─ log: "[NOTIFICATION] Appointment #N scheduled.
                                                  Patient: X | Doctor: Y | At: Z"
```

The main API returns `201 Created` before the notification is processed. If NotificationService is down, the message stays in the queue and is delivered when it comes back up — **no data loss, no tight coupling**.

---

### Message Contract

```csharp
public record AppointmentScheduledEvent(
    int AppointmentId,
    int PatientId,
    string PatientFullName,
    int DoctorId,
    string DoctorFullName,
    DateTime ScheduledAt,
    int DurationMinutes,
    DateTime OccurredAt
);
```

MassTransit auto-names the queue from the consumer type: `clinic-management-notification-service_appointment-scheduled`.

---

### Running Locally (Phase 3)

Run **all 3 services** simultaneously:

```bash
dotnet run --project src/ClinicManagement.AuthService
dotnet run --project src/ClinicManagement.API
dotnet run --project src/ClinicManagement.NotificationService
```

RabbitMQ must be running on `localhost:5672` (default guest/guest credentials).
Management UI available at `http://localhost:15672`.

---

### The Phase 3 Lesson

**What async decoupling buys:** The main API's response time is unaffected by how long notification delivery takes. NotificationService can be deployed, restarted, or scaled independently. New consumers (e.g., an AuditService) can subscribe to the same event without touching the API.

**What's now painful:** Three processes to run locally. The notification is fire-and-forget — if the consumer throws an error, the API has already returned success. You need dead-letter queues and retry policies for production. That operational complexity is intentional — you now understand *why* teams invest in it.

**Why MassTransit over raw RabbitMQ client (ADR-005):** The consumer doesn't know it's talking to RabbitMQ. Swap the transport to Azure Service Bus or Amazon SQS with one line in `Program.cs`.

---

## Phase 4 — API Gateway (YARP)

> Tag: `phase-4-api-gateway` | Branch: `Phase4_APIGateway`

### Goal
Give clients a single entry point. Before Phase 4, callers needed to know three different ports. After Phase 4, everything goes through the Gateway — services can move, scale, or be replaced without clients noticing.

---

### What Changed

#### New Project: `ClinicManagement.Gateway`
A minimal ASP.NET Core app with zero business logic. The entire Gateway is 3 lines of C# and a routing config block in `appsettings.json`.

```csharp
// Program.cs — the entire gateway implementation
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

Everything else — route matching, load balancing, header forwarding, health checks — is YARP configuration.

---

### Routing Table

| Path prefix | Forwards to | Service |
|---|---|---|
| `/auth/**` | `https://localhost:7026` | `ClinicManagement.AuthService` |
| `/api/**` | `https://localhost:7119` | `ClinicManagement.API` |

Clients talk only to the Gateway on `https://localhost:7161`.

---

### Architecture After Phase 4

```
Client (browser / mobile / Postman)
  │
  └─► https://localhost:7161  (ClinicManagement.Gateway)
            │
            ├─ /auth/**  ──────────────► AuthService       :7026
            │                              └─ ClinicManagement_Auth DB
            │
            └─ /api/**   ──────────────► ClinicManagement.API  :7119
                                           ├─ ClinicManagement DB
                                           └─ publishes to RabbitMQ
                                                  │
                                           NotificationService :7xxx
                                              (no HTTP port needed)
```

---

### Running Locally (Phase 4)

Run all services. Only the Gateway port matters to clients:

```bash
dotnet run --project src/ClinicManagement.Gateway          # https://localhost:7161
dotnet run --project src/ClinicManagement.AuthService      # https://localhost:7026
dotnet run --project src/ClinicManagement.API              # https://localhost:7119
dotnet run --project src/ClinicManagement.NotificationService
```

Test via Gateway:
```
POST https://localhost:7161/auth/login
POST https://localhost:7161/api/appointments
GET  https://localhost:7161/api/patients
```

---

### The Phase 4 Lesson

**What the Gateway buys:** Clients are now decoupled from service topology. You can change AuthService's port, split the API into two services, or add a BillingService — all without touching client code. The Gateway is the only contract that matters externally.

**What's still painful:** The Gateway config (`appsettings.json`) must be kept in sync with actual service ports. In production this is solved by service discovery (Consul, Kubernetes DNS) — the Gateway asks "where is AuthService right now?" instead of having a hardcoded address. That's a Phase 6+ concern.

**Why YARP (ADR-006):** Native .NET, no extra infrastructure. A Node.js team might use nginx or Kong. A .NET team gets YARP in-process, same runtime, same logging pipeline.

---

---

## Phase 5 — Performance Layer

> Tag: `phase-5-performance-layer` | Branch: `Phase5_PerformanceLayer`

### Goal
Read performance is always the first bottleneck. Phase 5 introduces two standard production techniques — a read cache and a lightweight query executor — without changing the system's observable behavior or adding new endpoints.

---

### What Changed

#### Read/Write Split (CQRS matured)
Every query handler now has two dependencies instead of one:

| Before | After |
|---|---|
| `IPatientRepository` (EF Core, owns reads + writes) | `IPatientReadRepository` (Dapper, reads only) + `IPatientRepository` (EF Core, writes only) |

Writes still go through EF Core — change tracking, validation, and domain events all stay intact. Reads bypass EF Core entirely and execute raw SQL through Dapper.

#### Cache-Aside Pattern
Every list and detail query follows the same flow:

```
Request
  │
  ├─► Redis? ──yes──► return cached DTO
  │
  └─► no → Dapper SQL → SQL Server
                │
                └─► write to Redis (TTL 2–5 min) → return DTO
```

The cache key encodes all query parameters: `patients:page:after=0:size=20`, `appointments:42`. A write operation (create, update) should invalidate the relevant cache keys — that eviction logic lives in command handlers and is the natural next step.

#### ICacheService — Application Layer Abstraction
The Application layer defines the cache contract:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

`RedisCacheService` in Infrastructure implements it using `IDistributedCache`. The handler doesn't know it's talking to Redis.

#### Dapper Read Repositories
Raw SQL with explicit JOINs — no N+1, no lazy loading, no EF Core overhead:

```sql
SELECT a.Id, a.PatientId,
       p.FirstName + ' ' + p.LastName AS PatientFullName,
       a.DoctorId,
       d.FirstName + ' ' + d.LastName AS DoctorFullName,
       a.ScheduledAt, a.DurationMinutes, a.Status, ...
FROM Appointments a
INNER JOIN Patients p ON p.Id = a.PatientId
INNER JOIN Doctors d ON d.Id = a.DoctorId
WHERE a.Id > @AfterID
ORDER BY a.Id
OFFSET 0 ROWS FETCH NEXT @PageSize ROWS ONLY
```

Keyset pagination (`WHERE a.Id > @AfterID`) is retained — no offset drift at scale.

---

### Running Locally (Phase 5)

Install Redis (Windows — one option):

```powershell
# Option 1: Docker
docker run -d -p 6379:6379 redis

# Option 2: Chocolatey
choco install redis-64
redis-server
```

`appsettings.json` already defaults Redis to `localhost:6379`. No other config change needed.

---

### The Phase 5 Lesson

**Why Dapper for reads?** EF Core builds query plans at runtime and materializes full entity graphs. Dapper runs the exact SQL you write and maps straight to DTOs. For list endpoints called hundreds of times per minute, that difference is measurable. For writes (one row at a time, with validation and domain events), EF Core's overhead is negligible.

**Why Redis for cache?** SQL Server handles hundreds of queries per second comfortably. But it doesn't scale horizontally, and every query burns CPU and network. Redis answers in microseconds and is horizontally scalable. The cache-aside pattern keeps the logic simple: the DB is always authoritative; Redis is just a fast shortcut.

**The clean architecture invariant holds.** The Application layer now depends on `ICacheService` and `IPatientReadRepository` — both interfaces defined in Application, implemented in Infrastructure. The dependency arrow still points inward. The handler doesn't import a Redis or Dapper namespace.

---

---

## Phase 6 — Containerization

> Tag: `phase-6-containerization` | Branch: `Phase6_Containerization`

### Goal
The whole system — seven processes across three infrastructure dependencies — should start with one command and work identically on any machine. No "works on my machine." No manual SQL Server setup, no local RabbitMQ erlang cookie fights.

---

### What Changed

#### Dockerfile per service (multi-stage build)
Each service has its own `Dockerfile` using a two-stage build:

```dockerfile
# Stage 1: build (SDK image — large, not shipped)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
# Copy .csproj files first → restore → then copy src → publish
# This layer-caches NuGet restore as long as .csproj files don't change

# Stage 2: runtime (aspnet image — small, ~220MB, what you ship)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080   # HTTP only inside the cluster
ENTRYPOINT ["dotnet", "ClinicManagement.API.dll"]
```

All services use HTTP internally. HTTPS termination belongs at the load balancer / ingress — not at each service.

#### docker-compose.yml
Seven containers, all wired together:

| Container | Image | Host Port |
|---|---|---|
| `sqlserver` | `mssql/server:2022-latest` | 1434 (avoids clash with local SQL Server on 1433) |
| `rabbitmq` | `rabbitmq:3-management` | 5673 / 15673 |
| `redis` | `redis:7-alpine` | 6380 |
| `auth-service` | built from source | 7026 |
| `clinic-api` | built from source | 7119 |
| `notification-service` | built from source | — (no HTTP port needed) |
| `gateway` | built from source | 7161 |

All services pass config via environment variables, which override `appsettings.json`. Container names are the hostnames: `rabbitmq`, `sqlserver`, `redis`, `auth-service`, `clinic-api`.

#### Health checks + depends_on
Infrastructure containers declare healthchecks. Application services use `depends_on: condition: service_healthy` — so `clinic-api` won't start until SQL Server is actually ready to accept connections, not just "started".

```yaml
sqlserver:
  healthcheck:
    test: sqlcmd -S localhost -U sa -P '...' -Q 'SELECT 1'
    interval: 10s
    retries: 10
    start_period: 30s

clinic-api:
  depends_on:
    sqlserver:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
    redis:
      condition: service_healthy
```

#### Auto-migrate on startup
`API` and `AuthService` call `db.Database.Migrate()` at startup:

```csharp
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
db.Database.Migrate();
```

On first `docker-compose up`, the databases don't exist. EF Core creates them and applies all migrations. On every subsequent restart it's a no-op. No manual `dotnet ef database update` step.

#### Gateway config for Docker
`appsettings.Docker.json` overrides cluster addresses from `localhost:7026` to `http://auth-service:8080` — using Docker's internal DNS. This file is loaded when `ASPNETCORE_ENVIRONMENT=Docker`, which is set in the Gateway's `Dockerfile`.

---

### Running the Full System

Prerequisites: Docker Desktop running.

```bash
# From the solution root
docker-compose up --build

# First run takes ~3–5 minutes (pulling images, compiling all projects)
# Subsequent runs: ~30 seconds (build cache hits)
```

Test via the Gateway:
```
POST http://localhost:7161/auth/register
POST http://localhost:7161/auth/login
POST http://localhost:7161/api/patients
GET  http://localhost:7161/api/patients
```

Stop everything:
```bash
docker-compose down          # stop containers, keep volumes
docker-compose down -v       # stop containers AND delete data
```

---

### The Phase 6 Lesson

**Why Docker Compose for development?** It eliminates "works on my machine." A new developer clones the repo and runs `docker-compose up`. There's no setup guide to follow, no versions to match, no Erlang cookie to fight. The compose file *is* the setup guide, and it's executable.

**Why HTTP inside the cluster?** HTTPS requires certificates. In production, a load balancer (nginx, Kubernetes ingress, AWS ALB) terminates TLS and forwards plain HTTP to services. Running HTTPS between services inside a private network is operational overhead with no security benefit — they're on the same trusted network. The Gateway is the only place that faces the outside world.

**Why offset host ports (1434, 5673, 6380)?** If you're running local SQL Server, RabbitMQ, or Redis for development outside Docker, the default ports are taken. Offset ports let you run `docker-compose up` alongside your local dev tooling without conflicts.

**What's deliberately left out:** production-grade Docker practices (secrets management, read-only filesystems, non-root users, resource limits, health-check HTTP probes) — those are Day 2 operations concerns. This phase proves the system is containerizable; hardening is a separate concern.

---

---

## Phase 7 — React UI

> Tag: `phase-7-react-ui`

### Goal
Add a browser-based frontend that talks exclusively through the Gateway. No service is called directly — all traffic flows through `http://localhost:7161`.

---

### Stack

| Choice | Reason |
|---|---|
| Vite + React + TypeScript | Fast dev server, HMR, type safety |
| Plain CSS | No UI library dependency — keeps focus on architecture, not styling |
| `localStorage` for JWT | Simple; production would use `httpOnly` cookies |

---

### Project Location

```
src/
└── ClinicManagement.Web/     # Vite React app
    ├── src/
    │   ├── api/
    │   │   └── client.ts     # All fetch calls + TypeScript interfaces
    │   ├── pages/
    │   │   ├── LoginPage.tsx       # Login + Register
    │   │   ├── PatientsPage.tsx    # List + Create patients
    │   │   ├── DoctorsPage.tsx     # List + Create doctors
    │   │   └── AppointmentsPage.tsx # List + Create appointments
    │   ├── App.tsx           # Sidebar layout, auth gate, page routing
    │   └── App.css           # All styles
    └── package.json
```

---

### Features

| Page | What it does |
|---|---|
| Login / Register | Issues JWT via AuthService, stores in `localStorage` |
| Patients | List all patients (paginated), create new patient |
| Doctors | List all doctors, create new doctor with specialty |
| Appointments | List appointments with patient + doctor names and status badge, create new appointment |

---

### Architecture Note — CORS

The Gateway was the only change needed on the backend:

```csharp
// ClinicManagement.Gateway/Program.cs
app.UseCors(policy =>
    policy.WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod());
```

Individual services (AuthService, ClinicManagement.API) needed no changes — they're only reachable through the Gateway inside the Docker network.

---

### Running the UI

The backend must be running first:
```bash
docker-compose up --build
```

Then in a separate terminal:
```bash
cd src/ClinicManagement.Web
npm install
npm run dev
```

Open **`http://localhost:5173`** in a browser.

---

### The Phase 7 Lesson

**Why all calls go through the Gateway:** The React app has one `BASE_URL` — `http://localhost:7161`. It has no knowledge of which port auth-service or clinic-api run on, or even that they're separate processes. Swapping a service, moving it to a different port, or replacing it entirely requires zero changes in the frontend. That's the Gateway's contract.

**Why `localStorage` and not cookies:** `httpOnly` cookies are the production-safe choice (immune to XSS). `localStorage` is used here because it's transparent — you can inspect the JWT directly in DevTools. The switch to cookies is a one-line change in production.

**Cache invalidation surfaced naturally:** After creating an appointment, the list showed stale data until the Redis TTL expired. This is the classic cache invalidation problem — the write path must also invalidate the relevant cache keys. Phase 8 material.

---

## The Journey — From Monolith to Microservices

You've built a system that evolved through seven architectural phases:

```
Phase 1: One process, one DB, clean internal structure
Phase 2: Auth extracted → two processes, JWT boundary
Phase 3: Async messaging → RabbitMQ decouples appointment creation from notification
Phase 4: API Gateway → YARP gives clients a single entry point
Phase 5: Performance layer → Redis + Dapper, reads separated from writes
Phase 6: Containerization → docker-compose up starts everything
Phase 7: React UI → browser frontend talks only to the Gateway
```

Each phase added one architectural idea. Each transition exposed real pain — the erlang cookie fight, the ControllerBase naming conflict, the design-time EF migration failure, the Dapper DateOnly mismatch, the MassTransit license wall. That pain *is* the lesson. Production systems carry all of it at once.

The codebase on `master` is a working microservice system with clean architecture, JWT authentication, async messaging, an API gateway, a read cache, and full container orchestration — built incrementally, one concept at a time.
