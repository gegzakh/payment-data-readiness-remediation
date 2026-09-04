# Payment Data Readiness & Remediation

Platform that helps banks find, fix, test and evidence payment-party address data that fails current or
upcoming payment-scheme validation (starting with the end of unstructured addresses in EPC schemes).

- Business and functional requirements: `docs/BUSINESS_REQUIREMENTS.md`, `docs/FUNCTIONAL_REQUIREMENTS.md`
- Architecture and delivery phases: `docs/architecture/ARCHITECTURE.md`
- Layering, patterns and per-service design: `docs/architecture/LOW_LEVEL_DESIGN.md`
- Repository conventions for contributors and agents: `AGENTS.md`

## What is implemented

| Piece | Location |
| --- | --- |
| Shared building blocks (errors, results, logging, correlation, auth, persistence, settings, web defaults) | `backend/src/BuildingBlocks` |
| API gateway (YARP) | `backend/src/Gateway/PDR.Gateway` |
| Release Notes service (domain → API, EF migrations, seeding, unit/integration/security tests) | `backend/src/Services/ReleaseNotes` |
| Rules service (versioned, dated scheme rulesets) and Audit service (hash-chained ledger) | `backend/src/Services/{Rules,Audit}` |
| Sources service (inventory, ISO 20022 field mappings, lineage, scan coverage, owner attestation) | `backend/src/Services/Sources` |
| Ingestion service (upload safety, quarantine, ISO 20022 XML + CSV parsing, batch reconciliation) | `backend/src/Services/Ingestion` |
| Validation service (address classification, current vs post-cutover rules, readiness, payments at risk) | `backend/src/Services/Validation` |
| Remediation service (cases, proposals, maker-checker, campaigns, reversible write-back) | `backend/src/Services/Remediation` |
| Public web UI with the release-notes page | `frontend/web-ui` |
| Admin UI (release authoring, runtime settings) | `frontend/admin-ui` |
| Local stack: PostgreSQL, Keycloak, RabbitMQ, Redis, MinIO, Seq, MailHog | `deploy/docker-compose.yml` |

## Run it locally

```bash
docker compose -f deploy/docker-compose.yml up -d          # infrastructure + Keycloak realm import

cd backend
dotnet run --project src/Services/ReleaseNotes/src/PDR.ReleaseNotes.Api   # :5101, migrates + seeds on start
dotnet run --project src/Services/Rules/src/PDR.Rules.Api                 # :5102
dotnet run --project src/Services/Audit/src/PDR.Audit.Api                 # :5103
dotnet run --project src/Services/Sources/src/PDR.Sources.Api             # :5104
dotnet run --project src/Services/Ingestion/src/PDR.Ingestion.Api         # :5105
dotnet run --project src/Services/Validation/src/PDR.Validation.Api       # :5106
dotnet run --project src/Services/Remediation/src/PDR.Remediation.Api     # :5107
dotnet run --project src/Gateway/PDR.Gateway                              # :5100

cd ../frontend/web-ui && npm install && npm run dev        # :5173
cd ../admin-ui && npm install && npm run dev               # :5174
```

- API reference (Scalar): `/scalar/v1` on each service (`:5101`–`:5107`), OpenAPI document at
  `/openapi/v1.json`
- Admin UI: Readiness (portfolio readiness today and after the cutover, exposure profiles, record
  drill-down), Sources (inventory, mappings, lineage, attestation), Ingestion (upload, batches, parsed
  records), Remediation (funnel, case queue, corrections, evidence, approvals, bulk actions),
  Write-back (targets, preview, runs, reconciliation, rollback), Rules (versions, activation and rollback), Audit (ledger search and chain verification),
  Releases and Settings
- Sample payloads to upload on the Ingestion screen: `samples/pain.001-sample.xml` and
  `samples/parties.csv` (source code `HUB-EU` is seeded)
- Keycloak: <http://localhost:8080> (`admin`/`admin`); realm `pdr` ships `pdr-admin`/`pdr-admin`
  (all permissions), `pdr-user`/`pdr-user` (read only) and `pdr-checker`/`pdr-checker` (approves
  corrections another user submitted, and runs write-back)
- Health: `/health/live`, `/health/ready` on every service

## Checks

```bash
cd backend                                           # global.json selects the SDK and test runner
dotnet build PaymentDataReadiness.slnx -warnaserror
dotnet test PaymentDataReadiness.slnx                # integration tests need Docker

cd ../frontend/web-ui  && npm run lint && npm run typecheck && npm test && npm run build
cd ../admin-ui      && npm run lint && npm run typecheck && npm test && npm run build
```
