# Payment Data Readiness & Remediation

Platform that helps banks find, fix, test and evidence payment-party address data that fails current or
upcoming payment-scheme validation (starting with the end of unstructured addresses in EPC schemes).

- Business and functional requirements: `docs/BUSINESS_REQUIREMENTS.md`, `docs/FUNCTIONAL_REQUIREMENTS.md`
- Architecture and delivery phases: `docs/architecture/ARCHITECTURE.md`
- Layering, patterns and per-service design: `docs/architecture/LOW_LEVEL_DESIGN.md`
- Running the platform (startup order, health, migrations, recovery, tracing): `docs/OPERATIONS.md`
- Public API and webhook contract: `docs/architecture/PUBLIC_API.md`
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
| Simulation service (scenarios, reproducible runs, test plans, UAT, cutover and go/no-go) | `backend/src/Services/Simulation` |
| Reporting service (audience dashboards, drill-down, CSV export, freshness and reconciliation) | `backend/src/Services/Reporting` |
| Notifications service (subscriptions, signed webhooks/ITSM tasks, retries, scheduled reports) | `backend/src/Services/Notifications` |
| Public web UI with the release-notes page | `frontend/web-ui` |
| Admin UI (release authoring, runtime settings) | `frontend/admin-ui` |
| Local stack: PostgreSQL, Keycloak, RabbitMQ, Redis, MinIO, Seq, MailHog | `deploy/docker-compose.yml` |
| Everything above as containers (infrastructure + services + gateway + both UIs) | `deploy/docker-compose.full.yml` |

## Run everything in Docker

One command brings up infrastructure, all ten services, the gateway and both front ends (Docker Desktop
or Docker Engine with Compose v2.20+, ~8 GB of memory):

```bash
mkdir -p ~/Documents/Repo && cd ~/Documents/Repo
git clone https://github.com/gegzakh/payment-data-readiness-remediation.git
cd payment-data-readiness-remediation
git checkout dev
docker compose -f deploy/docker-compose.full.yml up -d --build   # first build takes a few minutes
```

| URL | What |
| --- | --- |
| <http://localhost:5173> | Public UI (release notes) |
| <http://localhost:5174> | Admin UI — sign in with `pdr-admin`/`pdr-admin` |
| <http://localhost:5100/scalar/v1> | API reference through the gateway |
| <http://localhost:8080> | Keycloak (`admin`/`admin`) |
| <http://localhost:5341> | Seq logs |

```bash
curl -f http://localhost:5100/health/ready                       # gateway; services are :5101-:5110
docker compose -f deploy/docker-compose.full.yml logs -f gateway
docker compose -f deploy/docker-compose.full.yml down            # stop; add -v to reset the databases
```

The passwords above are local development defaults from `deploy/keycloak/pdr-realm.json`; they are not
usable outside this compose stack, which binds every port to localhost only. Databases are created on
first start of an empty PostgreSQL volume by `deploy/postgres/init-databases.sh` — after adding a service,
recreate the volume with `down -v`.

## Run it from source

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
dotnet run --project src/Services/Simulation/src/PDR.Simulation.Api       # :5108
dotnet run --project src/Services/Reporting/src/PDR.Reporting.Api         # :5109
dotnet run --project src/Services/Notifications/src/PDR.Notifications.Api # :5110
dotnet run --project src/Gateway/PDR.Gateway                              # :5100

cd ../frontend/web-ui && npm install && npm run dev        # :5173
cd ../admin-ui && npm install && npm run dev               # :5174
```

- API reference (Scalar): `/scalar/v1` on each service (`:5101`–`:5110`), OpenAPI document at
  `/openapi/v1.json`
- Admin UI: Readiness (portfolio readiness today and after the cutover, exposure profiles, record
  drill-down), Sources (inventory, mappings, lineage, attestation), Ingestion (upload, batches, parsed
  records), Remediation (funnel, case queue, corrections, evidence, approvals, bulk actions),
  Write-back (targets, preview, runs, reconciliation, rollback), Simulation (scenarios, runs, run
  comparison), Testing (risk-based test plans, UAT reconciliation), Cutover (entry/exit criteria,
  go/no-go pack), Dashboards (executive, scheme, source, operations, remediation, testing, cutover with
  drill-down and CSV export), Notifications (subscriptions, deliveries, scheduled reports),
  Rules (versions, activation and rollback), Audit (ledger search and chain verification),
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

Smoke the running stack under concurrent load (k6, thresholds on the read surface and dashboards):

```bash
k6 run -e GATEWAY=http://localhost:5100 -e TOKEN="$ACCESS_TOKEN" deploy/load/smoke.js
```
