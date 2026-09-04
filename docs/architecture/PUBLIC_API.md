# Public API and webhooks (PDR-040)

Everything below is reachable through the gateway at `http://localhost:5100` in the dev stack. Each
service also serves its own Scalar reference at `/scalar/v1` and its OpenAPI document at
`/openapi/v1.json`.

## Versioning

- Every route is versioned in the path: `/api/v1/<service>/…`. A breaking change ships as `/api/v2/…`
  alongside `v1`; additive fields are not breaking and are added in place.
- Enum values are transmitted as their names (`"Critical"`, `"Webhook"`), never as ordinals, so adding
  a member does not shift existing values.
- Settings that change behaviour (page sizes, retry budgets, freshness windows) are readable and
  editable at `/api/v1/services/<service>/settings`.

## Authentication and scopes

Bearer tokens are issued by Keycloak (realm `pdr`). Authorization is permission-based; the token's
`permissions` claim must contain the permission the route requires.

| Area | Read | Write | Elevated |
| --- | --- | --- | --- |
| Simulation | `simulation.read` | `simulation.write` | — |
| Testing | `testing.read` | `testing.write` | — |
| Cutover | `cutover.read` | `cutover.write` | `cutover.approve` |
| Reporting | `reporting.read` | — | `reporting.export` |
| Notifications | `notifications.read` | `notifications.write` | `notifications.admin` |

`notifications.admin` guards the operations that can leak or replay data: rotating a signing secret,
replaying a delivery and triggering a dispatch pass.

## Idempotency

`POST /api/v1/notifications/events` is idempotent on a caller-supplied key, taken from the
`Idempotency-Key` header first and the body's `idempotencyKey` second. Re-publishing the same key
returns the original notification with its original fan-out instead of creating a second one, so a
client that retries after a timeout never double-notifies subscribers.

## Limits

| Limit | Setting | Default |
| --- | --- | --- |
| Event payload size | `notifications.max-payload-bytes` | 65 536 bytes (UTF-8) |
| Delivery attempts before dead-letter | `notifications.max-delivery-attempts` | 5 |
| Retry backoff ceiling | `notifications.max-backoff-minutes` | 60 |
| Dispatch batch size | `notifications.dispatch-batch-size` | 50 |
| Consecutive failures before a subscription is disabled | `notifications.disable-subscription-after-failures` | 20 |
| Page size / maximum page size | `notifications.page-size` | 20 / 200 |

Request rate limiting is applied by the shared building blocks; exceeding it returns `429` with a
ProblemDetails body. All errors are ProblemDetails carrying the stable error `code` (for example
`SUBSCRIPTION.INSECURE_TARGET`) and the correlation id.

## Webhooks

A webhook subscription needs an absolute HTTPS target (loopback is allowed for local development) and
a signing secret. The secret is never returned by the API; `hasSigningSecret` reports only that one is
set, and it can be replaced with `POST /subscriptions/{code}/secret`.

Each delivery is a `POST` of the notification envelope:

```json
{
  "id": "0195…",
  "eventType": "validation.completed",
  "severity": "Warning",
  "subject": "Validation finished for HUB-EU",
  "schemeCode": "SEPA",
  "sourceCode": "HUB-EU",
  "occurredAtUtc": "2026-04-01T09:00:00+00:00",
  "payload": { "…": "event specific" }
}
```

with headers:

| Header | Meaning |
| --- | --- |
| `PDR-Timestamp` | Unix seconds at signing time |
| `PDR-Signature` | `v1=<hex>` where `<hex>` is `HMAC-SHA256(secret, "{PDR-Timestamp}.{raw body}")` |

Verify by recomputing the HMAC over the *raw* body and comparing in constant time, and reject
timestamps outside your tolerance window to stop replays:

```csharp
var expected = WebhookSignature.Compute(secret, rawBody, DateTimeOffset.FromUnixTimeSeconds(timestamp));
var ok = CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(expected),
    Encoding.UTF8.GetBytes(receivedSignature));
```

### Retries

Any non-2xx response, timeout or transport error is a failed attempt. Attempts back off 1, 2, 4, 8 …
minutes up to the ceiling and then dead-letter once the budget is spent. Dead-lettered deliveries stay
queryable at `GET /deliveries?status=DeadLettered` and can be requeued with
`POST /deliveries/{id}/replay` after the endpoint is fixed. Consumers must be idempotent: a delivery
that timed out after the receiver committed will be retried.

A target that fails `notifications.disable-subscription-after-failures` times in a row is disabled so
one dead endpoint cannot hold up the queue; re-enabling it clears the counter.

## Scheduled reports

`POST /api/v1/notifications/scheduled-reports` registers a dashboard delivery on a daily, weekly or
monthly cadence. The next slot is computed from the schedule itself, so a paused or restarted service
never floods recipients with missed windows — the report simply lands at the next slot. Runs publish a
`report.<audience>` event, so the same subscription, signing and retry rules apply to them.
