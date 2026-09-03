# Operations runbook

How the platform is started, watched and recovered. Architecture is in
`docs/architecture/ARCHITECTURE.md`; the public contract is in `docs/architecture/PUBLIC_API.md`.

## 1. Startup order

1. `docker compose -f deploy/docker-compose.yml up -d` — PostgreSQL, Keycloak, RabbitMQ, Redis.
2. Backend services (any order — they do not call each other at startup).
3. Gateway (`:5100`), then the front ends (`:5173` public, `:5174` admin).

Reporting composes Validation, Remediation and Simulation. If one of them is down, the dashboard is
still served but its `reconciliation` field degrades to `Partial` or `Unreconciled`; the number is
never silently completed from a stale upstream.

| Service | Port | Database |
| --- | --- | --- |
| Gateway | 5100 | — |
| Release Notes | 5101 | `pdr_releasenotes` |
| Rules | 5102 | `pdr_rules` |
| Audit | 5103 | `pdr_audit` |
| Sources | 5104 | `pdr_sources` |
| Ingestion | 5105 | `pdr_ingestion` |
| Validation | 5106 | `pdr_validation` |
| Remediation | 5107 | `pdr_remediation` |
| Simulation | 5108 | `pdr_simulation` |
| Reporting | 5109 | `pdr_reporting` |
| Notifications | 5110 | `pdr_notifications` |

## 2. Health and readiness

- `GET /health/live` — the process is up. Never touches the database; use it for restarts.
- `GET /health/ready` — migrations applied and dependencies reachable. Use it for load-balancer
  membership, and wait for it before running smoke tests.

Both are anonymous by design so probes need no token.

## 3. Migrations

Every service applies its own EF migrations at startup through `MigrationRunner<TContext>`, which
takes a PostgreSQL advisory lock first. Several replicas can therefore start simultaneously: one
migrates, the others wait and then continue. Consequences worth knowing:

- A service that cannot take the lock **waits**; it does not start serving a half-migrated schema.
- Databases themselves are not created by the app. `deploy/postgres/init-databases.sh` runs only on an
  empty `postgres-data` volume, so a branch that adds a service needs either
  `docker compose down -v` or `CREATE DATABASE pdr_<service>` by hand.
- Roll back by deploying the previous image; do not hand-edit generated migrations.

## 4. Runtime settings

Tunables come from `appsettings.json` and are overridden by database-backed settings, editable
without a restart at `PUT /api/v1/services/<service>/settings/<key>` (permission `settings.write`).
The ones that change behaviour under load:

| Key | Effect |
| --- | --- |
| `ReleaseNotes:Paging:DefaultPageSize`, `:MaxPageSize`, `:AllowedPageSizes` | Public page size, the ceiling a caller can ask for, and the sizes the page offers. |
| `reporting.freshness-seconds` | How long a dashboard snapshot is reused before upstreams are re-read. Raise it when upstreams are struggling. |
| `notifications.max-delivery-attempts`, `.max-backoff-minutes` | When a delivery is dead-lettered instead of retried. |
| `notifications.disable-subscription-after-failures` | When a persistently failing endpoint stops being called at all. |
| `notifications.dispatch-batch-size` | Deliveries per worker pass. |
| `Api:RateLimitPermitPerMinute` (appsettings) | Fixed-window limit per caller; rejections are `429` with ProblemDetails. |

## 5. Notifications: stuck or failing deliveries

1. `GET /api/v1/notifications/deliveries?status=DeadLettered` — what failed and the last error.
2. Fix the endpoint, then `POST /api/v1/notifications/deliveries/{id}/replay` (permission
   `notifications.write`). Replay resets the attempt counter; it does not duplicate the event.
3. A subscription disabled after repeated failures is re-enabled with
   `POST /api/v1/notifications/subscriptions/{code}/enabled`.
4. If the receiver rejects signatures, rotate the secret with
   `POST /api/v1/notifications/subscriptions/{code}/secret` (permission `notifications.admin`) and
   update it on their side; signatures are `v1=HMAC-SHA256(secret, "{timestamp}.{body}")`.
5. Where the dispatch worker is switched off (`Worker:Enabled=false` in appsettings), nothing is sent until
   `POST /api/v1/notifications/deliveries/dispatch` is called.

## 6. Diagnosing a failed request

Every response carries `X-Correlation-Id` (echoed from the caller when supplied), and every error is
RFC 9457 problem+json with a stable `code`, the `correlationId` and, for validation failures, an
`errors` map. To trace: take the correlation id from the response or the UI, then grep the structured
logs of the gateway and the owning service for it. Unhandled exceptions never leak stack traces, SQL
or personal data — the log holds the detail, the caller gets `COMMON.UNEXPECTED_ERROR`.

Business-relevant actions are additionally recorded in the Audit ledger, which is hash-chained;
`GET /api/v1/audit/verify` reports the first sequence number that no longer matches, which is how a
change made directly in the database is detected.

## 7. Smoke and load

```sh
k6 run -e GATEWAY=http://localhost:5100 -e TOKEN="$ACCESS_TOKEN" deploy/load/smoke.js
```

`deploy/load/smoke.js` runs a small constant load across the read surface plus the executive
dashboard. It is a smoke test, not a benchmark: it asserts that nothing fails under concurrency and
that the p95 stays inside the thresholds. Keep the virtual-user count low enough that a single caller
stays under `Api:RateLimitPermitPerMinute` — a `429` in this run means the limiter is misconfigured
rather than that the platform is slow.

## 8. Backup and restore

Each service owns its database and they are only consistent with each other in a business sense, so
back up all of them at the same point in time (`pg_dump` per database, or a filesystem/volume
snapshot). Order matters on restore only for readability: Rules and Sources first, then the
transactional services. Audit is append-only — restoring an older copy is detectable by design and
must be recorded as an erratum.
