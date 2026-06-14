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
| 5 | — | Performance Layer — Redis cache, Dapper reads, CQRS matured | 🔜 |
| 6 | — | Containerization — Docker Compose, full local orchestration | 🔜 |

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

*Each phase will add a new section to this file. By Phase 6, this README will tell the complete story of how a monolith becomes a microservice system.*
