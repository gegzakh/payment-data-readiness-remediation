---
name: local-e2e-testing
description: How to bring up the full PDR stack locally (infra, backend services, both front ends) and drive end-to-end/browser tests, including Keycloak logins, seeded data and adversarial permission checks. Use when manually or automatically testing PDR features end to end.
---

# Local end-to-end testing of the PDR stack

## 1. Infrastructure

```sh
docker compose -f deploy/docker-compose.yml up -d
```

Postgres databases are created only by `deploy/postgres/init-databases.sh`, which runs **only when the
`postgres-data` volume is empty**. If a branch adds a new service database (e.g. `pdr_rules`,
`pdr_audit`), an existing container will not have it, and the new service fails to start. Likewise a
branch that adds Keycloak roles will not have them until the realm is re-imported. Symptom: valid
admin token gets `403` from a brand new service. Fix (destroys local data, usually fine):

```sh
docker compose -f deploy/docker-compose.yml down -v
docker compose -f deploy/docker-compose.yml up -d
```

Alternatively create just the missing DBs:
`docker exec <postgres-container> psql -U pdr -d postgres -c "CREATE DATABASE pdr_<service>"`.

## 2. Backend services

```sh
export DOTNET_ROOT=/home/ubuntu/.dotnet && export PATH=$DOTNET_ROOT:$PATH
cd backend    # global.json here selects the SDK / test runner
nohup dotnet run --project src/Gateway/PDR.Gateway > /tmp/gw.log 2>&1 &                       # :5100
nohup dotnet run --project src/Services/ReleaseNotes/src/PDR.ReleaseNotes.Api > /tmp/rn.log 2>&1 &  # :5101
# one process per service; ports follow the service list in README
```

Readiness: `curl -s localhost:<port>/health/ready`. Services auto-migrate and seed on first start.

## 3. Front ends

```sh
nohup npm --prefix frontend/web-ui run dev > /tmp/web.log 2>&1 &     # :5173 public
nohup npm --prefix frontend/admin-ui run dev > /tmp/admin.log 2>&1 & # :5174 admin (Keycloak)
```

Always browse `http://localhost:5173` / `http://localhost:5174` directly. A tunnelled/external
hostname yields Vite's "Blocked request. This host is not allowed." page.

## 4. Auth

Realm `pdr` on `http://localhost:8080`; users `pdr-admin`/`pdr-admin` (all permissions) and
`pdr-user`/`pdr-user` (read only). For adversarial API checks obtain tokens directly (never reuse
browser cookies):

```sh
curl -s -X POST http://localhost:8080/realms/pdr/protocol/openid-connect/token \
  -d client_id=pdr-web -d username=pdr-admin -d password=pdr-admin -d grant_type=password \
  | python3 -c 'import json,sys;print(json.load(sys.stdin)["access_token"])'
```

Then call through the gateway: `curl -H "Authorization: Bearer $T" http://localhost:5100/api/v1/...`.
Expect `403` + ProblemDetails for read-only writes and `409` + a `code` for domain-state violations.

## 5. Seeding test data

Some services deliberately seed nothing (e.g. the audit ledger seeds only settings). To exercise
search/paging/verification, append records through the authorised API first, e.g.
`POST /api/v1/audit` with `{service,action,entityType,entityId,outcome,actor}` — vary service/actor/
outcome so filters have something to distinguish. Default audit page size is 20.

## 6. Known pitfalls when driving the admin UI

- **Rules page "Add rule" version selector can be stale.** `RulesAdminPage` keeps the target version in
  React state initialised to `1` and does not update it when a new draft is created, so the `<select>`
  may *display* the new draft while the POST still goes to the old version → visible
  `409 RULESET.VERSION_IMMUTABLE`. Workaround while testing: after creating a draft, explicitly pick the
  version in the selector (fire a real change event) before submitting. Similar stale-state traps may
  exist on other pages that create then immediately act on new entities.
- Read-only users may see silently empty admin tables when a query 403s (pages don't render `isError`).
- Tamper-evidence check: edit a row behind the app's back
  (`UPDATE audit_records SET actor='tampered' WHERE sequence=N`), press *Verify chain integrity* → the
  UI should say "Broken at sequence N"; restore the value to get back to "Intact".

## Devin Secrets Needed

None — all credentials are local dev defaults from `deploy/keycloak/pdr-realm.json`.
