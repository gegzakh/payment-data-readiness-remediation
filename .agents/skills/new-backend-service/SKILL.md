---
name: new-backend-service
description: Scaffold a new PDR microservice with the shared building blocks, EF Core persistence, auto-migration and tests. Use when adding any backend service to this repository.
---

# Adding a PDR microservice

Mirror `backend/src/Services/ReleaseNotes` — it is the reference implementation.

## Layout

```
backend/src/Services/<Service>/
  src/PDR.<Service>.Domain          # aggregates, value objects, domain events, no dependencies
  src/PDR.<Service>.Application     # commands/queries + handlers, DTOs, validators, abstractions
  src/PDR.<Service>.Infrastructure  # DbContext, EF configurations, migrations, seeders, integrations
  src/PDR.<Service>.Api             # minimal API endpoints, Program.cs
  tests/PDR.<Service>.UnitTests
  tests/PDR.<Service>.IntegrationTests
```

Register every project in `backend/PaymentDataReadiness.slnx` (`dotnet sln ... add`).

## Wiring checklist

1. `Program.cs`: `builder.AddPdrService("<service>")`, `Add<Service>Application()`,
   `Add<Service>Infrastructure(builder.Configuration)`, then `app.UsePdrDefaults()`,
   `app.Map<Service>Endpoints()`, `app.MapSettingsEndpoints()`. Expose `public partial class Program;`
   so integration tests can host it.
2. Infrastructure: `AddPdrPersistence<TContext>(configuration)` (gives DbContext, unit of work,
   settings provider, migration runner), plus `IDataSeeder` implementations for defaults.
3. `DbContext` derives from `BaseDbContext` (audit stamping, soft delete, outbox, row version).
   Entity configurations call `ConfigureAuditColumns()` and `UseRowVersionConcurrencyToken()`.
4. Generate the migration in the repo:
   `dotnet ef migrations add <Name> -p <Infrastructure> -s <Api> -o Persistence/Migrations`.
5. Give the service its own database in `deploy/postgres/init-databases.sh` and a route + cluster in
   `backend/src/Gateway/PDR.Gateway/appsettings.json` (one port per service, 51xx).
6. Handlers return `Result`/`Result<T>`; endpoints translate with `ToHttpResult` / `ToCreatedResult`.
   Never throw for expected failures.

## Tests

- Unit: domain invariants and pure application logic (xUnit v3 + AwesomeAssertions + NSubstitute).
  Pass `TestContext.Current.CancellationToken` to async calls.
- Integration: subclass the pattern in `ReleaseNotesApiFactory` — a `PostgreSqlContainer` plus
  `WebApplicationFactory<Program>`, configured with `builder.UseSetting(...)` (in-memory config sources
  are overridden by the app's own appsettings.json, `UseSetting` is not).
- Security: a second factory with `Authentication:Keycloak:Enabled=true` asserting anonymous and forged
  tokens are rejected while public endpoints stay open.
