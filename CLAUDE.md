# CLAUDE.md — Clinic Management System
## Architectural Guardrails & Decision Record

> This file is the single source of truth for all architectural decisions in this repository.
> Before writing any code, read the relevant section. Before making an exception, update this file with the reason.

---

## Project Purpose

This is a **learning-first** repository. Every architectural decision is made to maximize
understanding of microservice patterns — not to ship the fastest possible product.

The evolution is intentional:
```
Phase 1: Clean Monolith
Phase 2: Auth Service Extracted
Phase 3: Async Messaging (RabbitMQ + Notifications)
Phase 4: API Gateway (YARP) + Billing Extracted
Phase 5: Performance Layer (Redis, CQRS, Indexing)
Phase 6: Containerization (Docker Compose)
```

**Do not skip phases. The pain of each phase is the lesson.**

---

## Repository Structure

```
ClinicManagement/
├── src/
│   ├── ClinicManagement.API/
│   ├── ClinicManagement.Application/
│   ├── ClinicManagement.Domain/
│   ├── ClinicManagement.Infrastructure/
│   └── ClinicManagement.Shared/
├── tests/
│   ├── ClinicManagement.UnitTests/
│   └── ClinicManagement.IntegrationTests/
├── client/
│   └── clinic-web/               # React + Vite
├── docs/
│   └── decisions/                # ADR files (see below)
└── CLAUDE.md                     # This file
```

---

## Layer Dependency Rules — NON-NEGOTIABLE

```
Domain          → depends on nothing
Application     → depends on Domain only
Infrastructure  → depends on Application + Domain
API             → depends on Application only
```

### What this means in practice

| Allowed | Forbidden |
|---|---|
| API references Application interfaces | API references Infrastructure directly |
| Application defines repository interfaces | Application references EF Core |
| Infrastructure implements Application interfaces | Domain references any other layer |
| Domain contains business rules | Domain references DTOs or ViewModels |

**If you find yourself adding an EF Core `using` in the API project — stop. You are violating the dependency rule.**

---

## Domain Layer Rules

### Entities
- Entities use **private setters**. State changes happen through methods only.
- Domain logic (validation, state transitions) lives **inside the entity**, not in services.
- Entities never reference DTOs, ViewModels, or external concerns.

```csharp
// CORRECT — behavior on the entity
public void Cancel()
{
    if (!CanCancel()) throw new DomainException("Cannot cancel.");
    Status = AppointmentStatus.Cancelled;
}

// WRONG — logic leaked into service
appointmentService.SetStatus(appointment, AppointmentStatus.Cancelled);
```

### Value Objects
- Use `record` types for value objects (immutable by design).
- Value objects have no identity — equality is by value, not by ID.

### Domain Exceptions
- All domain rule violations throw `DomainException` (defined in Domain layer).
- Never throw `ArgumentException` or `InvalidOperationException` for domain violations.

---

## Application Layer Rules

### Command/Query Structure
- Every use case is a **separate class** in its own folder.
- Folder structure: `Application/{Module}/Commands/{UseCaseName}/` and `Application/{Module}/Queries/{UseCaseName}/`
- One handler per use case. Handlers do not call other handlers.

### DTOs
- Commands and Queries carry their own input models.
- Responses always return DTOs — never return domain entities from handlers.
- DTOs live in `Application/Common/DTOs/` or co-located with the use case.

### Repository Interfaces
- All repository interfaces are defined in `Application/Common/Interfaces/`.
- Handlers depend on interfaces, never on concrete implementations.

```csharp
// CORRECT
public class GetPatientByIdQueryHandler
{
    private readonly IPatientRepository _repo; // interface from Application
}

// WRONG
public class GetPatientByIdQueryHandler
{
    private readonly PatientRepository _repo; // concrete from Infrastructure
}
```

### Validation
- Input validation uses **FluentValidation**.
- Validators are co-located with their Command/Query class.
- Never validate in controllers. Never validate in domain entities (that is domain logic, not input validation).

---

## Infrastructure Layer Rules

### EF Core
- Every entity has a dedicated `IEntityTypeConfiguration<T>` class.
- No Fluent API configuration in `OnModelCreating` directly — use `ApplyConfigurationsFromAssembly`.
- **All indexes are defined in configuration classes, not as data annotations.**
- `ChangeTracker.Clear()` is called after every batch insert during seeding.

### Repository Pattern
- Repositories implement interfaces defined in the Application layer.
- Repositories return domain entities or primitives — never `IQueryable`.
- No business logic in repositories. Filtering, ordering, pagination only.

### Database
- Primary database: **SQL Server**
- ORM: **EF Core 8** for standard operations
- **Dapper** for complex read queries on large datasets (Phase 5 onwards)
- All migrations are code-first via EF Core CLI

---

## API Layer Rules

### Controllers
- Controllers are thin. They:
  1. Accept input
  2. Validate (via FluentValidation pipeline)
  3. Dispatch to Application handler (via MediatR)
  4. Return HTTP response
- No business logic in controllers. Ever.
- Controllers never call repositories directly.

### Error Handling
- Global exception handling via `ExceptionHandlingMiddleware`.
- `DomainException` → 400 Bad Request
- `NotFoundException` → 404 Not Found
- Unhandled exceptions → 500 Internal Server Error (with correlation ID in response)

### Response Format
All API responses follow this envelope:
```json
{
  "success": true,
  "data": {},
  "error": null,
  "correlationId": "uuid"
}
```

### Pagination
- **All list endpoints are paginated. No exceptions.**
- Use **keyset pagination** (not offset) for large datasets.
- Default page size: 20. Maximum page size: 100.

```csharp
// WRONG — breaks at scale
var patients = context.Patients.Skip(page * size).Take(size).ToList();

// CORRECT — keyset pagination
var patients = context.Patients
    .Where(p => p.Id > lastSeenId)
    .Take(size)
    .ToList();
```

---

## Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Commands | `{Verb}{Entity}Command` | `CreatePatientCommand` |
| Queries | `Get{Entity}Query` | `GetPatientByIdQuery` |
| Handlers | `{CommandOrQuery}Handler` | `CreatePatientCommandHandler` |
| DTOs | `{Entity}Dto` | `PatientDto` |
| Repositories | `I{Entity}Repository` | `IPatientRepository` |
| Services | `I{Name}Service` | `INotificationService` |
| Exceptions | `{Reason}Exception` | `DomainException`, `NotFoundException` |
| DB Configs | `{Entity}Configuration` | `PatientConfiguration` |

---

## Git Conventions

### Commit message format
```
{phase}: {type}: {short description}

Examples:
phase1: feat: add Patient entity and configuration
phase1: fix: correct index on Patient email column
phase2: feat: extract Auth service with JWT issuance
phase3: feat: publish AppointmentBooked event via MassTransit
```

### Tags
Each completed phase is tagged before starting the next:
```
git tag phase-1-complete
git tag phase-2-complete
```

**Never rewrite history on main. Tags are your restore points.**

---

## Performance Rules (active from Phase 1)

- **Indexes are not optional.** Every foreign key column and every column used in WHERE/ORDER BY gets an index.
- **Batch inserts only.** Never insert records one by one in seeders or bulk operations.
- **No SELECT \*.** Always project to the columns you need.
- **No lazy loading.** EF Core lazy loading is disabled. Use explicit `.Include()` or separate queries.
- **Pagination on every list endpoint.** See API Rules above.

---

## What Changes Per Phase (and What Doesn't)

| Rule | Phase 1 | Phase 2+ |
|---|---|---|
| Dependency direction | Same | Same — never changes |
| Domain logic in entities | Same | Same — never changes |
| Thin controllers | Same | Same — never changes |
| Database per service | Single DB | Each extracted service gets its own DB |
| Auth | None (open endpoints) | JWT issued by Auth service |
| Communication | In-process | HTTP (sync) or RabbitMQ (async) |
| Entry point for React | Direct to API | Via YARP Gateway (Phase 4) |

---

## Architecture Decision Records (ADRs)

All significant decisions are documented in `docs/decisions/`.

### ADR format
```
# ADR-{number}: {Title}
**Date:** YYYY-MM-DD
**Status:** Accepted / Superseded by ADR-{n}

## Context
Why did this decision need to be made?

## Decision
What was decided?

## Consequences
What does this make easier? What does this make harder?
```

### Decisions already made

| ADR | Decision | Reason |
|---|---|---|
| ADR-001 | Clean Architecture layering | Enables surgical service extraction in later phases |
| ADR-002 | CQRS folder structure from Phase 1 | Use case isolation without MediatR overhead initially |
| ADR-003 | Keyset pagination over offset | Offset pagination degrades at scale with 1M+ records |
| ADR-004 | Database-per-service from Phase 2 | Enforces true service autonomy; shared DB is coupling |
| ADR-005 | MassTransit over raw RabbitMQ client | Broker abstraction; swap transport without code changes |
| ADR-006 | YARP as API Gateway | Native .NET; no additional infrastructure required |
| ADR-007 | Defer containerization to Phase 6 | Avoid compounding Docker complexity during architecture learning |
| ADR-008 | Bogus for seed data | Realistic data distributions expose real query performance issues |
| ADR-009 | Dapper for heavy reads (Phase 5+) | EF Core materializes full entities; Dapper projects exactly what is needed |

---

## Things Explicitly Out of Scope

- **gRPC** — HTTP/REST is sufficient for learning service communication patterns
- **Kubernetes** — Docker Compose is the containerization target
- **Event Sourcing** — Outbox pattern is used, full event sourcing is not
- **GraphQL** — REST with proper resource modeling is the standard here
- **Multiple databases** (e.g., MongoDB for some services) — SQL Server throughout for consistency

These are not wrong choices in general. They are excluded to keep the learning surface focused.

---

## The One Rule That Overrides Everything Else

> **If a shortcut makes the code easier to write today but harder to extract tomorrow, it is not allowed.**

Every decision in Phase 1 is made with Phase 2 extraction in mind.
Every decision in Phase 2 is made with Phase 4 gateway introduction in mind.

The seams must be clean before the cut happens.
