# Payment Data Readiness & Remediation — Target Architecture (Proposal v0.1)

Derived from `docs/BUSINESS_REQUIREMENTS.md` (BRD v1.0) and `docs/FUNCTIONAL_REQUIREMENTS.md` (FRD v1.0).
Status: **approved 3 Sep 2026** — Keycloak as IdP, React front-ends, docker-compose only (no Kubernetes at this stage), full phased build.

## 1. Reality check on scope

The FRD backlog (PDR-001…PDR-046, 46 stories, 8 epics, 34 core entities) describes a multi-team enterprise programme.
This proposal delivers it as a **real, runnable, end-to-end system** built in phases, where every phase is
independently deployable and demonstrable, rather than a mock of everything at once.

Phase plan (each phase = a working system, PR per phase):

| Phase | Content | FRD coverage |
|---|---|---|
| P0 | Solution skeleton, shared building blocks, docker-compose infra (PostgreSQL, RabbitMQ, Redis, Seq), gateway, CI | NFR-004/006/007/010 |
| P1 | Identity & Access, Rules & reference data, **Release Notes**, Audit ledger, Admin UI + Web UI shells | PDR-001…005, PDR-041 (partial) |
| P2 | Sources & lineage, Ingestion (ISO 20022 XML + CSV), Validation engine, profiling, drill-down | PDR-006…017 |
| P3 | Remediation cases, proposals, maker-checker workflow, campaigns, write-back + reconciliation | PDR-018…029 |
| P4 | Simulation, test management, cutover/go-no-go, reporting dashboards, notifications, public API/webhooks | PDR-030…040 |
| P5 | Hardening: load tests, security tests, DR/backup scripts, observability SLOs, accessibility audit | PDR-042…046 |

The existing static demo (`index.html`, `_next/`) stays untouched at the repo root and remains the published
GitHub Pages demo; the platform lives in new top-level folders (see §6).

## 2. Architectural style

- **Microservices**, one bounded context each, each owning its own PostgreSQL **database** (not just a schema),
  communicating synchronously over HTTP (through the gateway or service-to-service) and asynchronously over
  **RabbitMQ** (MassTransit) for cross-context events and long-running work.
- **Clean/hexagonal layering inside each service**: `Domain` → `Application` → `Infrastructure` → `Api`/`Worker`.
- **CQRS-lite** (MediatR commands/queries, no separate event-sourced write model) — full event sourcing is
  deliberately rejected as unjustified complexity; auditability is met by the append-only Audit ledger + EF temporal-style
  history tables.
- **Transactional outbox** for every state change that publishes an event (FR-AUD, NFR-006 "no silent partial success").
- Async work (parse a 2 GB file, run a simulation, run a write-back batch) executes in **worker services** consuming
  RabbitMQ queues, never in the request path.

## 3. Services

| # | Service | Responsibility (FRD) | Storage | Async |
|---|---|---|---|---|
| 1 | `Gateway` (YARP) | Routing, JWT validation, rate limiting, correlation ID, aggregated Swagger/Scalar | – | – |
| 2 | `Identity` | Keycloak is the OIDC/OAuth2 provider (realm, users, MFA, federation). This service owns platform-side access data: role→permission maps, ABAC scopes (legal entity/scheme/source), service-account registry, maker-checker policy, and Keycloak admin-API synchronisation | `pdr_identity` | publishes user/role events |
| 3 | `Rules` | Schemes, rule sources, rulesets, versions, effective dating, reference data, draft-vs-active impact compare, activation/rollback | `pdr_rules` | publishes `RulesetActivated` |
| 4 | `Sources` | Source-system inventory, owners, interfaces, field mappings, lineage paths, attestation, readiness | `pdr_sources` | consumes rule events → reassessment tasks |
| 5 | `Ingestion` (+ worker) | Upload/SFTP/API/DB ingestion, file safety checks, quarantine, batch lifecycle, ISO 20022 XML + CSV parsing, checkpointing, idempotency | `pdr_ingestion` + object storage (MinIO/S3) | consumes `IngestBatch`, publishes `BatchParsed` |
| 6 | `Validation` (+ worker) | Deterministic rule evaluation (current + future), address classification, issue detection, count reconciliation, profiling, low-latency pre-submission API | `pdr_validation` | consumes `BatchParsed`, publishes `BatchValidated` |
| 7 | `Remediation` (+ worker) | Deduplicated cases, proposals with per-field confidence/evidence, maker-checker workflow, exceptions, campaigns, write-back jobs, read-after-write reconciliation, rollback | `pdr_remediation` | consumes `BatchValidated`, publishes `CaseRemediated`, `WriteBackCompleted` |
| 8 | `Simulation` (+ worker) | Scenarios, projections, comparisons, test plans/executions/defects, cutover plans, entry/exit criteria, go/no-go pack | `pdr_simulation` | consumes validation/remediation events |
| 9 | `Reporting` | Denormalised read models fed by events; dashboards, drill-down, as-of/exclusion metadata, scheduled reports, exports | `pdr_reporting` | consumes all domain events |
| 10 | `Notification` | In-app + email + webhook fan-out, templates, SLA reminders, escalation | `pdr_notification` | consumes all notable events |
| 11 | `Audit` | Append-only, hash-chained audit ledger; retention, legal hold, evidence-pack export | `pdr_audit` | consumes `audit.*` from every service |
| 12 | `ReleaseNotes` | Releases, entries (feature/change/fix), grouping by release date/logical part, descending order, configurable page size, publish workflow | `pdr_releasenotes` | publishes `ReleasePublished` |

Rationale for the split: it follows the FRD's own capability boundaries (§3.1–3.11), keeps the high-write ingestion/validation
path independently scalable (NFR-008), and isolates the audit ledger for tamper-evidence (FR-AUD-003).

## 4. Cross-cutting concerns (identical in every service, via shared building blocks)

- **Error handling:** every unhandled path funnels through one exception-handling middleware producing
  RFC 9457 `application/problem+json` with `traceId`, `correlationId`, error `code`, and safe messages
  (no stack traces / no PII). Domain errors use a `Result<T>` type; only infrastructure faults throw.
- **Logging:** Serilog structured logging → console (JSON) + Seq/OTLP, with enrichers for `correlationId`,
  `userId`, `tenant/legalEntity`, `service`, `version`; PII destructuring policy masks address/party fields.
- **Tracing/metrics:** OpenTelemetry (ASP.NET Core, HttpClient, EF Core, MassTransit, Npgsql, Redis) → OTLP.
- **AuthN/AuthZ:** JWT bearer validated at gateway *and* service; policy-based authorization with permission claims
  (`rules.activate`, `case.approve`, …), ABAC over legal entity / scheme / source scopes, maker-checker enforced in the
  Application layer, not the UI.
- **Configuration:** `appsettings.json` + environment overrides + a `SystemSettings` table per service exposed through
  `/api/v1/settings` endpoints (typed, validated, cached in Redis, hot-reloaded via `IOptionsMonitor`).
  Precedence: DB setting → environment variable → appsettings.
- **Caching:** Redis for active rulesets, reference data, permission snapshots, settings, idempotency keys, rate limits.
- **API docs:** OpenAPI (built-in .NET 10 `Microsoft.AspNetCore.OpenApi`) + **Scalar** UI at `/scalar` per service and
  aggregated at the gateway; Swagger UI kept as a secondary route `/swagger`.
- **Migrations:** EF Core **code-first**, applied automatically at startup by a migration runner
  (advisory-lock guarded, so only one instance migrates; `MigrateAsync()` behind `Database:AutoMigrate=true`).
- **Health:** `/health/live`, `/health/ready` (DB, RabbitMQ, Redis probes).

## 5. Technology and dependencies

**Backend (.NET 10, C# 14)**

| Concern | Package |
|---|---|
| Web | ASP.NET Core Minimal APIs + endpoint groups |
| ORM | `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Mediator/CQRS | `MediatR` (pipeline behaviours: validation, logging, transaction, audit, idempotency) |
| Validation | `FluentValidation` |
| Messaging | `MassTransit` + `MassTransit.RabbitMQ` (+ EF outbox) |
| Cache | `StackExchange.Redis` / `Microsoft.Extensions.Caching.StackExchangeRedis` |
| Logging | `Serilog.AspNetCore`, `Serilog.Sinks.Seq`, `Serilog.Sinks.Console` |
| Telemetry | `OpenTelemetry.Extensions.Hosting` + instrumentation packages |
| Auth | Keycloak 26 (OIDC provider), `Microsoft.AspNetCore.Authentication.JwtBearer` (all services), `Keycloak.AuthServices.Authentication`/admin client in Identity |
| Docs | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` |
| Resilience | `Microsoft.Extensions.Http.Resilience` (Polly v8) |
| Mapping | Manual mapping / `Mapster` (no AutoMapper) |
| Object storage | `AWSSDK.S3` against MinIO in dev |
| Rate limiting | built-in `Microsoft.AspNetCore.RateLimiting` + Redis store |

**Testing**

| Level | Stack |
|---|---|
| Unit | `xUnit v3`, `FluentAssertions`, `NSubstitute`, `Bogus` |
| Integration | `Testcontainers` (PostgreSQL, RabbitMQ, Redis), `WebApplicationFactory`, `Respawn` |
| Contract | MassTransit `TestHarness`, gateway route tests |
| Security | authorization matrix tests, XXE/billion-laughs/zip-bomb parser tests, IDOR tests, mass-assignment tests |
| Frontend | `Vitest` + Testing Library; `Playwright` for E2E |
| Coverage gate | Coverlet, ≥80% line on Domain/Application layers (Infrastructure exempt) |

**Frontend — recommendation: React 19 + TypeScript + Vite** (over Vue) because the ecosystem for the
data-grid-heavy, permission-driven screens this product needs (TanStack Table/Query, virtualized grids,
shadcn/ui, react-hook-form + zod) is stronger and hiring/maintenance is easier in banking teams.

Two separately deployable apps in one pnpm workspace, sharing packages:

```
frontend/
  apps/web-ui     # Operations: dashboards, batches, validation results, cases, campaigns, release notes (public page)
  apps/admin-ui   # Administration: users/roles, rules & reference data, sources, settings, release-notes authoring, audit
  packages/ui         # design system (Tailwind + shadcn/ui), a11y-checked (WCAG 2.2 AA)
  packages/api-client # generated from OpenAPI (openapi-typescript + orval)
  packages/auth       # oidc-client-ts wrapper, PKCE, silent renew, permission guards
  packages/config     # env/runtime config loader
```

Both apps hit the same gateway; splitting them keeps the admin surface deployable behind stricter network controls.

**Infrastructure (dev):** `docker-compose` with PostgreSQL 17, Keycloak 26 (pre-seeded `pdr` realm, clients and roles),
RabbitMQ 4 (management), Redis 7, MinIO, Seq and MailHog. Kubernetes/Helm is explicitly out of scope for now.

**CI:** GitHub Actions — build, unit+integration tests (Testcontainers), lint (`dotnet format`, ESLint, Prettier),
frontend build, `dotnet list package --vulnerable`, CodeQL, Trivy image scan.

## 6. Repository layout

```
/                         # existing static demo (untouched)
/docs                     # BRD, FRD, architecture, ADRs, runbooks
/backend
  PaymentDataReadiness.sln
  /src
    /BuildingBlocks       # Core, Domain, Application, Persistence, Messaging, Security, Observability, WebApi
    /Gateway
    /Services
      /Identity/{src,tests}
      /Rules/{src,tests}
      /Sources/{src,tests}
      /Ingestion/{src,tests}
      /Validation/{src,tests}
      /Remediation/{src,tests}
      /Simulation/{src,tests}
      /Reporting/{src,tests}
      /Notification/{src,tests}
      /Audit/{src,tests}
      /ReleaseNotes/{src,tests}
  /tests                  # cross-service integration & security suites
/frontend                 # pnpm workspace (see above)
/deploy                   # docker-compose, Dockerfiles, k8s manifests/helm, seed data
/.agents/skills           # project skills & conventions (per your request)
```

Each service folder has the requested `src` / `tests` split; within `src`:
`X.Domain`, `X.Application`, `X.Infrastructure`, `X.Api`, and `X.Worker` where async work exists.

## 7. Release Notes (explicit requirement)

- Service `ReleaseNotes` owns `Release` (version, title, releaseDate, status draft/published, summary) and
  `ReleaseEntry` (type = Feature | Change | Fix | Security | Breaking, component/logical part, title, description, ticket refs, order).
- Public read API: `GET /api/v1/releases?page=1&pageSize=20&type=&component=&from=&to=` — sorted by
  `releaseDate DESC, version DESC`, entries grouped by **logical part** within each release.
- Page size default and allowed values come from `SystemSettings` (`ReleaseNotes:DefaultPageSize` = 20,
  `ReleaseNotes:AllowedPageSizes` = [10,20,50]) — changeable via settings endpoint without redeploy.
- Web UI: `/release-notes` page (anonymous-readable), keyset+offset pagination, filters, deep links per release.
- Admin UI: authoring, draft/publish workflow, markdown body, preview; publishing emits `ReleasePublished`
  → Notification service → subscribers.

## 8. Key architecture decisions (ADRs to be recorded)

1. Database-per-service on one PostgreSQL cluster (isolation without operational sprawl).
2. RabbitMQ + MassTransit with transactional outbox/inbox; no distributed transactions — sagas for write-back.
3. Keycloak as the external OIDC provider (realm-per-environment, PKCE for SPAs, client credentials for service accounts); services are pure resource servers validating JWTs — FR-ADM-001.
4. React over Vue (see §5); two front-ends, one gateway.
5. Deterministic rule engine (compiled specification objects + versioned rule definitions in DB) — never AI (BRD §11).
6. Auto-migrate on startup with advisory lock; destructive migrations require explicit opt-in flag.
7. Scalar as primary API docs UI, Swagger UI kept for familiarity.

## 9. Decisions taken

1. **Delivery**: full phased build, P0 → P5, one PR per phase.
2. **Auth**: Keycloak.
3. **Frontend**: React 19 + TypeScript + Vite, two apps.
4. **Deployment**: docker-compose only for now; Kubernetes deferred.
5. **Data**: publicly documented ISO 20022 message shapes and 100% synthetic fixtures — no production data (BRD §11).
