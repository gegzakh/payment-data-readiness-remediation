# Payment Data Readiness & Remediation — working rules

Architecture and design decisions live in `docs/architecture/ARCHITECTURE.md` and
`docs/architecture/LOW_LEVEL_DESIGN.md`. Read them before adding a service.

## Non-negotiables

- Backend: .NET 10, PostgreSQL, EF Core code-first. Migrations are generated in the repo and applied
  automatically at startup by `MigrationRunner<TContext>` under a PostgreSQL advisory lock.
- Every service is a folder under `backend/src/Services/<Service>` with `src/` and `tests/` inside it,
  and projects named `PDR.<Service>.{Domain,Application,Infrastructure,Api}`.
- Cross-cutting behaviour (error handling, ProblemDetails, logging, correlation, auth, rate limiting,
  OpenAPI/Scalar, health checks) comes from `PDR.BuildingBlocks.*`; never re-implement it per service.
  A service's `Program.cs` is `AddPdrService` + `UsePdrDefaults` plus its own registrations.
- Authentication is Keycloak (JWT bearer). Authorize with `RequireAuthorization(Permissions.X.Y)`;
  add new permissions to `PDR.BuildingBlocks.Security/Permissions.cs` and the realm import.
- Tunables are `appsettings.json` defaults overridden by DB-backed settings (`ISettingsProvider`),
  editable at runtime through `/api/v1/settings`.
- Frontends are React + TypeScript (Vite): `frontend/web-ui` (public) and `frontend/admin-ui`
  (Keycloak-authenticated). They talk only to the gateway.
- Kubernetes manifests are out of scope for now; `deploy/docker-compose.yml` is the dev stack.

## Definition of done for a change

1. `dotnet build backend/PaymentDataReadiness.slnx -warnaserror` and `dotnet test backend/PaymentDataReadiness.slnx` pass.
2. Frontend touched? `npm run lint && npm run typecheck && npm test && npm run build` in that app.
3. Tests that carry value: domain rules and pure logic as unit tests, HTTP behaviour and persistence as
   integration tests against a Testcontainers PostgreSQL, authorization as security tests.
4. User-visible change? Add a release-notes entry (see `.agents/skills/release-notes/SKILL.md`).
