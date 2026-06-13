# Clinic Management System

A learning-first project that evolves a **Clean Architecture monolith** into a **microservice system**, one phase at a time.
Each phase introduces a new architectural concern. The pain of each transition is the lesson.

---

## Evolution Roadmap

| Phase | Tag | Theme | Status |
|---|---|---|---|
| 1 | `phase-1-monolith` | Clean Monolith — domain model, CQRS, EF Core, REST API | ✅ Complete |
| 2 | — | Auth Service Extracted — JWT issuance, cross-service HTTP | 🔜 |
| 3 | — | Async Messaging — RabbitMQ + Notifications via MassTransit | 🔜 |
| 4 | — | API Gateway — YARP reverse proxy, Billing service | 🔜 |
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

*Each phase will add a new section to this file. By Phase 6, this README will tell the complete story of how a monolith becomes a microservice system.*
