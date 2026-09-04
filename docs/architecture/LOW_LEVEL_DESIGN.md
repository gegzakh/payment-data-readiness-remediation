# Low-Level Design & Design Patterns (Proposal v0.1)

Companion to `ARCHITECTURE.md`. Status: **approved 3 Sep 2026** (Keycloak, React, docker-compose, full phased build).

## 1. Service internal structure (identical everywhere)

```
Services/<Name>/
  src/
    PDR.<Name>.Domain/         # entities, value objects, domain events, invariants, no dependencies
    PDR.<Name>.Application/    # commands, queries, handlers, validators, ports (interfaces), policies
    PDR.<Name>.Infrastructure/ # EF Core DbContext, configurations, migrations, repositories, adapters, consumers
    PDR.<Name>.Api/            # minimal-API endpoint groups, DI composition root, OpenAPI/Scalar
    PDR.<Name>.Worker/         # (where applicable) MassTransit consumers, schedulers, long-running jobs
  tests/
    PDR.<Name>.UnitTests/
    PDR.<Name>.IntegrationTests/
    PDR.<Name>.SecurityTests/   # only where an auth/parser/attack surface exists
```

Dependency rule: `Api|Worker → Infrastructure → Application → Domain`. Enforced by an
`ArchitectureTests` suite (NetArchTest) so the layering cannot silently rot.

## 2. Shared building blocks (`backend/src/BuildingBlocks`)

| Library | Contents |
|---|---|
| `PDR.BuildingBlocks.Core` | `Result`/`Result<T>`, `Error` (code, type, message, metadata), `PagedResult<T>`, `IClock`, guard clauses, `CorrelationContext` |
| `PDR.BuildingBlocks.Domain` | `Entity<TId>`, strongly-typed IDs, `AggregateRoot` with `DomainEvents`, `ValueObject`, `IAuditable`, `ISoftDeletable`, `IConcurrencyAware` (`xmin`/`RowVersion`) |
| `PDR.BuildingBlocks.Application` | MediatR pipeline behaviours: `ValidationBehavior`, `LoggingBehavior`, `TransactionBehavior`, `AuditBehavior`, `IdempotencyBehavior`, `AuthorizationBehavior`, `CachingBehavior`; `ICommand`, `IQuery`, `IPermissionChecker`, `IMakerCheckerPolicy` |
| `PDR.BuildingBlocks.Persistence` | `BaseDbContext` (audit stamping, soft delete filters, domain-event dispatch, outbox write), `OutboxMessage`, `InboxMessage`, `MigrationRunner` (Postgres advisory lock), `SystemSetting` store, keyset pagination helpers, JSONB converters |
| `PDR.BuildingBlocks.Messaging` | MassTransit setup, naming conventions, retry/redelivery/circuit-breaker policies, `OutboxPublisher`, integration-event contracts assembly `PDR.Contracts` |
| `PDR.BuildingBlocks.Security` | JWT bearer setup, permission constants, `RequirePermission` endpoint filter, ABAC scope evaluator, data-masking attributes/serializer, secrets provider abstraction |
| `PDR.BuildingBlocks.Observability` | Serilog + OpenTelemetry bootstrap, correlation middleware, health checks, `ActivitySource` per service |
| `PDR.BuildingBlocks.WebApi` | `ProblemDetails` exception middleware, `Result → IResult` mapping, versioning (`/api/v1`), rate limiting, Scalar/Swagger wiring, standard `Program.cs` composition extension `AddPdrService(...)` |
| `PDR.BuildingBlocks.Testing` | Testcontainers fixtures, `PdrWebApplicationFactory`, auth token helpers, Respawn reset, builders/fakes |

A new service is therefore ~30 lines of `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddPdrService("Rules");                  // logging, telemetry, auth, problem-details, settings, health
builder.AddPdrPersistence<RulesDbContext>();     // Npgsql, outbox, auto-migrate, interceptors
builder.AddPdrMessaging(cfg => cfg.AddConsumers(typeof(IRulesMarker).Assembly));
builder.Services.AddRulesApplication();          // MediatR, validators, policies

var app = builder.Build();
app.UsePdrDefaults();                            // correlation, exceptions, authn/z, rate limit, scalar+swagger
app.MapRulesEndpoints();
await app.RunPdrAsync();                         // migrate (advisory lock) then run
```

## 3. Design patterns used, and why

| Pattern | Where | Why |
|---|---|---|
| Clean/Hexagonal architecture | every service | testability, DB/broker swappable, enforced by arch tests |
| CQRS (MediatR) | every service | separates write invariants from read projections; behaviours give one place for cross-cutting rules |
| Mediator + Pipeline (Chain of Responsibility) | Application | validation/logging/txn/audit/idempotency applied uniformly (BRD "same rules in all services") |
| Result / Railway-oriented | Application, Domain | expected failures are values, not exceptions → deterministic error codes for API + UI |
| Repository + Unit of Work | Infrastructure | `DbContext` = UoW; repositories only where the aggregate needs a non-trivial query surface (no generic repo ceremony) |
| Specification | Rules & Validation | a `Rule` row compiles to a composable `ISpecification<AddressContext>`; AND/OR/NOT trees, explainable failures |
| Strategy + Factory | Validation, Ingestion | per-format parsers (`pain.001`, `pacs.008`, CSV layout) and per-rule-type evaluators resolved by keyed DI |
| Interpreter | Rules | effective-dated rule expressions (JSONB DSL) interpreted into specifications, cached in Redis by ruleset version |
| Template Method | Ingestion pipeline | `IngestionPipeline` fixes the steps (safety → parse → normalize → persist → publish); formats override steps |
| Transactional Outbox / Inbox | all publishers/consumers | exactly-once effect, no lost events (NFR-006) |
| Saga (MassTransit state machine) | Remediation write-back, Simulation runs | long-running: approve → write → read-after-write → replay message → revalidate → mark remediated, with compensation |
| Domain events → Integration events | all | inner consistency first, cross-service contracts explicit and versioned in `PDR.Contracts` |
| Options pattern + layered settings provider | all | `appsettings.json` ← env ← DB `SystemSettings`, hot reload via `IOptionsMonitor` |
| Decorator | caching, resilience | `CachedRulesetReader`, `ResilientSourceClient` wrap plain implementations |
| Policy/State machine | Workflow | `RemediationCase` status transitions declared in one table-driven policy with guard permissions (maker ≠ checker) |
| Idempotency key | write endpoints & consumers | `Idempotency-Key` header persisted per (key, endpoint, user), replays return the original response |
| Snapshot + audit hash chain | Audit service | each `AuditEvent` stores `prevHash`, `hash` → tamper evidence (FR-AUD-003) |
| Outbox-fed read models (materialized views) | Reporting | drillable, reconcilable metrics with `asOf` stamps (FR-REP-002) |
| Feature flags | all | phased rollout, safe defaults (NFR-010) |

## 4. Data model highlights

- Strongly-typed IDs (`readonly record struct RuleId(Guid Value)`), `Guid v7` for time-ordered keys.
- Every table: `Id`, `CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`, `RowVersion (xmin)`;
  business-critical tables additionally get a `*_history` table written by an EF interceptor
  (before/after JSONB) so original data is always recoverable (BRD rule 5).
- Effective dating on `Ruleset` (`ValidFrom`, `ValidTo`, `Status`: Draft|Approved|Active|Superseded|RolledBack)
  — never updated in place; activation writes a new version row + `Approval` record.
- `Address` modelled as a value object with `Structured` (Country, TownName, PostCode, StreetName,
  BuildingNumber, BuildingName, Floor, Room, District, SubDept, Dept) + `Unstructured` (AdrLine[1..7]) +
  `Classification` (Structured|Hybrid|Unstructured|Absent|Unrecognized).
- `ValidationResult` is immutable and always carries `RulesetVersionId`, `RuleId`, `Severity`,
  `Expected`, `Actual`, `EvidencePointer`, `EvaluatedAtUtc` (BRD rule 1).
- JSONB for rule expressions, proposal evidence, connector configs, report definitions; GIN-indexed.
- Partitioning by `IngestionBatchId`/month on the high-volume tables (`PaymentRecord`, `ValidationResult`).
- PII columns marked `[Sensitive]` → masked in logs, in exports, and for users lacking `data.view.full`.

## 5. Error handling contract (all services)

```json
{
  "type": "https://pdr.dev/errors/validation-failed",
  "title": "Validation failed",
  "status": 400,
  "code": "RULES.RULESET_NOT_DRAFT",
  "detail": "Ruleset 2026-11 is Active and cannot be edited.",
  "traceId": "00-...-01",
  "correlationId": "b3f4...",
  "errors": { "effectiveFrom": ["must be in the future"] }
}
```

Mapping: `ErrorType.Validation`→400, `Unauthorized`→401, `Forbidden`→403, `NotFound`→404,
`Conflict`/`Concurrency`→409, `Idempotency`→409/200-replay, `Unprocessable`→422, `RateLimited`→429,
`Dependency`→502/503, unexpected→500 with generic message. Consumers: retry policy → dead-letter queue
+ `IntegrationFailure` incident record; no silent swallow.

## 6. Testing strategy (value-first, not coverage theatre)

- **Unit** — domain invariants (rule effective dating, case state machine, address classification,
  confidence scoring), specification composition, mapping of rule DSL → specification, pagination math.
- **Integration** (Testcontainers) — EF mappings + migrations apply from scratch, outbox publishes,
  consumer idempotency, endpoint auth matrix, settings precedence, keyset pagination correctness,
  read-after-write reconciliation saga happy/compensating paths.
- **Security** — XXE / external entity / billion-laughs / oversized-file / zip-bomb parser tests,
  authorization matrix per role × endpoint, maker=checker rejection, IDOR across legal entities,
  mass-assignment, rate-limit enforcement, masked-field leakage tests.
- **Contract** — `PDR.Contracts` message schema snapshot tests to prevent breaking consumers.
- **Frontend** — component tests for permission-gated rendering; Playwright E2E for the golden paths
  (login → upload batch → see validation results → open case → approve → see release notes pagination).

## 7. Release Notes low-level design

```
Release(Id, Version, Title, ReleaseDateUtc, Status{Draft,Published,Archived}, Summary, PublishedAtUtc, PublishedBy)
ReleaseEntry(Id, ReleaseId, Type{Feature,Change,Fix,Security,Breaking}, Component, Title, Body(md), SortOrder, TicketRefs[])
```

- `GET /api/v1/releases` → `PagedResult<ReleaseListItem>` ordered `ReleaseDateUtc DESC, Version DESC`;
  `page`, `pageSize` (validated against `ReleaseNotes:AllowedPageSizes`, default `ReleaseNotes:DefaultPageSize`),
  filters `type`, `component`, `from`, `to`, `q`; response includes `totalCount`, `totalPages`, `asOfUtc`.
- `GET /api/v1/releases/{version}` → release with entries grouped by `Component` then `Type`.
- Admin: `POST/PUT/DELETE /api/v1/releases`, `.../entries`, `POST .../publish` (requires `releasenotes.publish`).
- Published releases are immutable except for an appended erratum entry (audit-friendly).
- Response cached in Redis (`releases:list:{hash}`) with invalidation on publish.
- UI: server-driven pagination, page-size selector limited to the configured allowed values, RSS/JSON feed optional.

## 8. Definition of done per story (mirrors FRD §7)

Acceptance criteria met · unit + integration tests green · authorization & audit test present where applicable ·
error paths covered · OpenAPI updated · settings documented · logs/metrics/traces emitted with correlation ·
migration applies cleanly from empty DB · rollback path for data-changing features · a11y check for new UI ·
release-notes entry added for the change.

## 9. Proposed first implementation PR (Phase 0 + start of Phase 1)

1. `backend` solution, `Directory.Build.props`/`Directory.Packages.props` (central package management, nullable, warnings-as-errors, analyzers).
2. All BuildingBlocks libraries with unit tests.
3. `deploy/docker-compose.yml` (PostgreSQL, RabbitMQ, Redis, MinIO, Seq, MailHog) + `.env.example`.
4. `Gateway` (YARP) with aggregated Scalar and auth passthrough.
5. `ReleaseNotes` service end-to-end (domain → API → tests) as the reference implementation of every pattern above.
6. `frontend` workspace with `web-ui` shell + `/release-notes` page and `admin-ui` shell + authoring screen.
7. GitHub Actions CI, `.agents/skills/` with the project conventions so future sessions follow them automatically.

Subsequent PRs follow the phase plan in `ARCHITECTURE.md` §1.
