# Payment Data Readiness & Remediation

Platform that helps banks find, fix, test and evidence payment-party address data that fails current or
upcoming payment-scheme validation (starting with the end of unstructured addresses in EPC schemes).

- Business and functional requirements: `docs/BUSINESS_REQUIREMENTS.md`, `docs/FUNCTIONAL_REQUIREMENTS.md`
- Architecture and delivery phases: `docs/architecture/ARCHITECTURE.md`
- Layering, patterns and per-service design: `docs/architecture/LOW_LEVEL_DESIGN.md`
- Repository conventions for contributors and agents: `AGENTS.md`

## What is implemented (phase 0)

| Piece | Location |
| --- | --- |
| Shared building blocks (errors, results, logging, correlation, auth, persistence, settings, web defaults) | `backend/src/BuildingBlocks` |
| API gateway (YARP) | `backend/src/Gateway/PDR.Gateway` |
| Release Notes service (domain → API, EF migrations, seeding, unit/integration/security tests) | `backend/src/Services/ReleaseNotes` |
| Public web UI with the release-notes page | `frontend/web-ui` |
| Admin UI (release authoring, runtime settings) | `frontend/admin-ui` |
| Local stack: PostgreSQL, Keycloak, RabbitMQ, Redis, MinIO, Seq, MailHog | `deploy/docker-compose.yml` |

## Run it locally

```bash
docker compose -f deploy/docker-compose.yml up -d          # infrastructure + Keycloak realm import

cd backend
dotnet run --project src/Services/ReleaseNotes/src/PDR.ReleaseNotes.Api   # :5101, migrates + seeds on start
dotnet run --project src/Gateway/PDR.Gateway                              # :5100

cd ../frontend/web-ui && npm install && npm run dev        # :5173
cd ../admin-ui && npm install && npm run dev               # :5174
```

- API reference (Scalar): <http://localhost:5101/scalar/v1>, OpenAPI document at `/openapi/v1.json`
- Keycloak: <http://localhost:8080> (`admin`/`admin`); realm `pdr` ships `pdr-admin`/`pdr-admin`
  (all permissions) and `pdr-user`/`pdr-user` (read only)
- Health: `/health/live`, `/health/ready` on every service

## Checks

```bash
cd backend                                           # global.json selects the SDK and test runner
dotnet build PaymentDataReadiness.slnx -warnaserror
dotnet test PaymentDataReadiness.slnx                # integration tests need Docker

cd ../frontend/web-ui  && npm run lint && npm run typecheck && npm test && npm run build
cd ../admin-ui      && npm run lint && npm run typecheck && npm test && npm run build
```
