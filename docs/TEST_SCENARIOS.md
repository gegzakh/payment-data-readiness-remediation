# Test scenarios

End-to-end scenarios for exercising the platform by hand, in the order a bank would actually use it:
find the exposure, fix it, prove the fix, then govern it. Each scenario states **the problem it solves**,
the **sample data** to use, the **steps**, and **what to assert**.

Prerequisites: the stack is up (`docs/OPERATIONS.md` or the README quick start), the admin UI is on
<http://localhost:5174> and the gateway on <http://localhost:5100>.

## Test users

| User | Password | Holds | Use it to prove |
| --- | --- | --- | --- |
| `pdr-admin` | `pdr-admin` | every permission | the happy path |
| `pdr-user` | `pdr-user` | read-only | write actions are refused (403) and buttons are hidden |
| `pdr-checker` | `pdr-checker` | `remediation.approve`, `remediation.writeback`, reads | four-eyes approval and write-back |

A token for API steps:

```bash
token() { curl -s -X POST http://localhost:8080/realms/pdr/protocol/openid-connect/token \
  -d client_id=pdr-web -d grant_type=password -d username="$1" -d password="$1" \
  | python3 -c 'import json,sys;print(json.load(sys.stdin)["access_token"])'; }
T=$(token pdr-admin); C=$(token pdr-checker)
```

Every failure is RFC 7807 `application/problem+json` carrying a stable `code`, `traceId` and
`correlationId` — assert on `code`, not on the message text.

## Sample data

`samples/parties.csv` — ten party addresses, deliberately mixed so one file produces every outcome:

| Record | Party | Shape | Expected finding |
| --- | --- | --- | --- |
| MSG-0001 | Northwind Trading BV (NL), Alpine Metals GmbH (DE) | fully structured | compliant today **and** after cutover |
| MSG-0002 | Riverstone Ltd (GB) | unstructured only (`221B Baker Street, London NW1 6XE`) | rejected after cutover — the headline problem |
| MSG-0002 | Costa Verde SA (ES) | hybrid, no post code | warning/rejected after cutover |
| MSG-0003 | Unknown Counterparty | no country, no address | unable to assess / rejected today |
| MSG-0004 | Maison Lefevre SARL (FR) | post code but no town or street | rejected after cutover |
| MSG-0005 | ultimate debtor/creditor pair | structured | proves ultimate parties are assessed too |

`samples/pain.001-sample.xml` — five parties inside a real `pain.001.001.09` message, for the ISO 20022
parser path.

Seeded reference data worth knowing: sources `HUB-EU`, `ERP-AP`, `CRM-MDM`; schemes `SEPA`
(structured addresses mandatory from `2026-11-15`), `CBPR`, `DOMESTIC`; simulation scenarios
`BASE-CURRENT`, `BASE-FUTURE`, `BASE-REMEDIATED`; cutover plan `CUTOVER-2026`; write-back targets
`HUB-EU`, `CBS` and `CRM`.

---

## Scenario 1 — Do we even know where the addresses live?

**Problem solved:** you cannot report readiness for data you have not inventoried. This scenario proves
the platform tracks every system holding payment-party addresses, its ISO 20022 mapping, and whether a
named owner has recently attested to it.

**Steps** (admin UI → **Assess → Sources**, as `pdr-admin`):

1. Read the metric strip: `Sources`, `Ready`, `Blocked`, `Attestation overdue`, `Parties covered`,
   `Avg readiness`.
2. Tick **Attestation overdue only**. `ERP-AP` appears — it has never been attested.
3. Click a source code to expand **field mappings** and **Lineage**.
4. On `ERP-AP` click **Record scan**, enter `75.5`, then click **Attest**.

**Assert:** `ERP-AP` readiness rises and it drops out of the overdue filter. Readiness is
`mapping (max 25) + attestation (25 if current) + coverage (÷2 fresh, ÷4 stale)`, capped at 100, so an
attestation plus a fresh scan is visibly worth more than a stale scan.

**Guard rails to trip:**

| Action | Expected |
| --- | --- |
| `POST /api/v1/sources/{code}/scan` with `coveragePercent: 250` | `400 SOURCE.INVALID_SCAN_COVERAGE` |
| Add the same source attribute + ISO element twice | `409 SOURCE.DUPLICATE_MAPPING` |
| Attest as `pdr-user` | `403` — `sources.attest` required |

---

## Scenario 2 — Get the data in safely

**Problem solved:** payment data arrives as files from systems you do not control. Ingestion has to
parse both CSV and ISO 20022, refuse hostile or malformed files, never silently double-count, and
account for every record it accepted.

**Steps** (admin UI → **Assess → Ingestion**):

1. Upload `samples/parties.csv` with **Source code** `HUB-EU`, **Format** `Csv`, click **Upload**.
2. Upload `samples/pain.001-sample.xml` with **Format** `Iso20022Xml`.
3. Open a batch to see the parsed records.

**Assert:** the CSV batch reaches `Parsed` with `Records 10 / Parsed 10 / Failed 0` and
**Reconciles** = yes. Reconciliation is the equation `recordCount == parsedCount + failedCount +
excludedCount` — it is the control that proves nothing was quietly dropped. The XML batch parses 5
records.

**Guard rails to trip:**

| Action | Expected | Why it matters |
| --- | --- | --- |
| Upload the same file, same source, again | `409 BATCH.DUPLICATE_SCAN` | the same file cannot inflate your exposure numbers twice |
| Repeat it with **Reprocess** ticked | succeeds, `isReprocess: true` | deliberate re-runs stay possible and are labelled |
| Upload twice with the same `idempotencyKey` | same batch id returned, nothing re-parsed | safe retries for automated feeds |
| Upload a file containing the EICAR test string | batch `Quarantined`, not parsed | malware never reaches the parser |
| Upload a file with `<!DOCTYPE` or `<!ENTITY` | rejected | XXE / entity-expansion attacks blocked |
| Upload `payload.bin` | rejected — allowed types are `.xml,.csv,.txt` | |
| **Cancel** a `Parsed` batch | `409 BATCH.ALREADY_PARSED` | |
| **Retry** a `Parsed` batch | `409 BATCH.NOT_RETRYABLE` | retry exists only for failures |
| Open batch records as `pdr-user` | party names, towns, post codes masked (`Ri*******`) | PII needs `validation.drilldown`; country is never masked |

---

## Scenario 3 — How exposed are we, today and after the cutover?

**Problem solved:** the business question is not "is this address valid" but "how many payments would
the scheme reject once unstructured addresses stop being accepted". This scenario produces that number
and makes it traceable to individual records.

**Steps:**

1. On **Ingestion**, click **Validate** on the parsed CSV batch (needs `validation.run`).
2. Go to **Assess → Readiness**.

**Assert** with `samples/parties.csv`:

- `Readiness today` ≈ **80%**, `Readiness after cutover` ≈ **40%** — the gap *is* the business case.
- `Payments at risk` = 2, `Rejected (future)` = 2.
- **Top issues** lists `ADDR.POSTCODE_REQUIRED` (Warning, Future) and `ADDR.TOWN_REQUIRED`
  (Error, Future) with record counts.

3. Change **Dimension** on **Exposure profile** through `Scheme`, `Source`, `Country`, `Classification`,
   `Issue`. Under `Issue` a record appears once per breached rule, so rows deliberately overlap.
4. Click a run's timestamp to drill into **Assessed records**, then filter by **Rule mode** =
   `Current` vs `Future`.

**Assert:** the same record can be `Compliant` today and `Rejected` after cutover — that is the whole
point of two rule modes. Riverstone Ltd (unstructured) is the clearest example.

**Guard rail:** sign in as `pdr-user` and drill in — addresses are masked and the page says
`validation.drilldown` is required. Every drill-down by a privileged user is auditable.

---

## Scenario 4 — Fix it, with four eyes and evidence

**Problem solved:** changing customer address data is a controlled act. The maker must not be the
checker, new data needs evidence, and every decision must be attributable. This scenario proves the
control, not just the edit.

> Verified live end-to-end; the exact responses below were observed on the running stack.

**Steps** (admin UI → **Remediate → Remediation**):

1. As `pdr-admin`, click **Generate cases from the latest validation runs**.
   **Assert:** with the CSV run, `assessmentsRead: 10`, `casesCreated: 3-4`; the funnel shows
   `Open cases` and `Exposure still open`. Repeat payments for one party fold into a single case with
   an occurrence count, so you fix a party once rather than a payment many times.
2. Open the `Riverstone Ltd` case. The proposal table shows **In the source today** vs **Proposed**.
3. Fill **Country** `GB`, **Town** `London`, **Post code** `NW1 6XE`, **Street** `Baker Street`,
   **Building** `221B` → **Save correction**.
4. Under **Evidence** add **Kind** `CustomerConfirmation`, **Reference** `DOC-4711` → **Attach**.
5. Click **Submit for approval**. **Assert:** status becomes `PendingApproval`.
6. Still as `pdr-admin`, try **Decision** = `Approve` → **Record decision**.
   **Assert:** `403 REMEDIATION.MAKER_CANNOT_CHECK` — *"The correction must be approved by somebody
   other than its maker."* This is the control working.
7. Sign out, sign in as `pdr-checker`, approve the same case with a rationale.
   **Assert:** status `Approved`; the **History** table shows both actors, both timestamps and the
   rationale.

**Guard rails to trip:**

| Action | Expected |
| --- | --- |
| **Submit for approval** with no proposal | `409 REMEDIATION.PROPOSAL_MISSING` |
| Submit a proposal that introduces values absent from the source, with no evidence | `409 REMEDIATION.EVIDENCE_REQUIRED` |
| `Return`/`Reject`/`Dismiss` without a rationale | `400 REMEDIATION.RATIONALE_REQUIRED` |
| `GrantException` without an expiry date | `400 REMEDIATION.EXCEPTION_NEEDS_EXPIRY` |
| Edit a `Remediated` or `Dismissed` case | `409 REMEDIATION.CASE_CLOSED` |
| Propose as `pdr-checker` | `403` — needs `remediation.write` |

**Known UI traps** (real, worth writing test steps around):

- Opening a case does **not** pre-fill existing proposal values — clicking **Save correction** on a
  blank form overwrites the proposal with empty values.
- The **Evidence** form does not reset when you switch cases; check the fields before clicking
  **Attach**.
- **Bulk approval** reuses the single-case **Rationale** input, and its preview is not recalculated
  when you change the filters — the count shown can be stale while **Approve N cases** acts on the
  current filter.

---

## Scenario 5 — Write the fix back, and be able to undo it

**Problem solved:** a correction that never reaches the source system is worthless, and one that cannot
be reversed is dangerous. This scenario proves write-back is previewed, verified by read-back, counted,
reconciled and reversible.

**Precondition:** a case from Scenario 4 has reached `Approved`. The sample data ingests under `HUB-EU`,
which is a seeded write-back target, so no extra setup is needed. (Databases created before this target
was seeded only hold `CBS` and `CRM`; recreate the volume with
`docker compose -f deploy/docker-compose.full.yml down -v` to pick it up.)

**Steps** (admin UI → **Remediate → Write-back**, as `pdr-checker`):

1. Read the **targets** table: `HUB-EU` and `CBS` are `Api` mode, 500 records per run, approval
   required, rollback by reverse write; `CRM` is `Export` mode with 200 records per run.
2. Click the `HUB-EU` source link to load it into the run form, then **Preview**.
   **Assert:** `eligibleCases: 1`, `recordsToWrite: 1`, and one `changes` row per field with
   `beforeValue: null` → `afterValue` (`town` → `London`, `postcode` → `NW1 6XE`, `street` →
   `Baker Street`, `buildingnumber` → `221B`). Only `Approved` cases are eligible.
3. Click **Apply 1 records**.
   **Assert:** job `status: Confirmed`, `itemCount 1`, `confirmedCount 1`, `failedCount 0`,
   `staleCount 0`. `Confirmed` (not just `Applied`) means the platform read the record back and saw its
   own value — write-back verifies itself.
4. Expand the run and click **Reconcile against the source**.
   **Assert:** `balanced: true`, `discrepancies: []`, and every item in a terminal bucket.
5. The case is now `Remediated`.
6. Enter a **Rollback reason** and click **Roll back**.
   **Assert:** job `RolledBack`, `rolledBackCount: 1`, and the case returns to `RolledBack` with
   `remediatedAtUtc` cleared — the correction is fully reversible.

**Guard rails to trip:**

| Action | Expected | Control proven |
| --- | --- | --- |
| Apply twice with the same `idempotencyKey` | the **same job id**, nothing written twice | a retried apply cannot double-write |
| Preview/apply for `ERP-AP` (a source with no target) | `409 WRITEBACK.TARGET_NOT_CONFIGURED` | only authorised targets are writable |
| Write back a case that is not `Approved` | `409 REMEDIATION.NOT_APPROVED` | approval gates the source write |
| Apply as `pdr-user` | `403` — `remediation.writeback` required | |

Note the UI generates a fresh idempotency key on each **Apply** click, so the replay test must be done
through the API.

---

## Scenario 6 — Would the fix actually move the number? (reproducibly)

**Problem solved:** readiness percentages get quoted to regulators and boards. They have to be
reproducible and traceable to the exact population and ruleset that produced them.

**Steps** (admin UI → **Prove → Simulation**):

1. Run `BASE-CURRENT` (today's rules), `BASE-FUTURE` (post-cutover rules) and `BASE-REMEDIATED`
   (future minus approved remediation) with the **Run** button.
2. Note each run's **Run key** — a SHA-256 over scenario code, mode, as-of date, scope filters,
   exclusions and ruleset version.
3. Run `BASE-CURRENT` a second time. Mark one run **Baseline** and the other **Candidate**, then
   **Compare**.
   **Assert:** `Same run key` is true and all deltas are `0` — identical inputs give identical results.
4. Compare `BASE-FUTURE` (baseline) with `BASE-REMEDIATED` (candidate).
   **Assert:** `Rejected delta` is negative and `Same run key` is false — this quantifies what
   remediation bought you.
5. Create a scenario of your own (**Code**, **Name**, **Mode**, **As of**, **Schemes** `SEPA`,
   **Sources** `HUB-EU`) → **Create**, then **Lock** it.

**Guard rails to trip:**

| Action | Expected |
| --- | --- |
| `PUT` a locked scenario | `409 SCENARIO.NOT_DRAFT` — locked definitions stay comparable |
| **Run** an archived scenario | `409 SCENARIO.ARCHIVED` |
| Create a duplicate code | `409 SCENARIO.DUPLICATE` |
| Compare a run that is not completed | `409 SIMULATION.NOT_COMPARABLE` |

---

## Scenario 7 — Prove it was tested, risk-weighted

**Problem solved:** "we tested it" is not evidence. Coverage must be weighted by risk, failures must
carry a defect reference, and the payment engine must be shown to agree with the platform on the same
sample.

**Steps** (admin UI → **Prove → Testing**):

1. **New plan**: **Code** `TP-SEPA`, **Name** `SEPA cutover UAT`, **Owner** `Test Manager` → **Create**.
2. **Add case** twice: `TC-1` (**Risk** `Critical`, **Scenario** `BASE-FUTURE`, **Expected** `Rejected`)
   and `TC-2` (**Risk** `Low`, **Expected** `Accepted`).
3. Click **Activate** on the plan.
4. **Record execution** for `TC-1`: **Status** `Failed`, **Actual** `Accepted`, leave **Defect** empty.
   **Assert:** `400 TESTPLAN.DEFECT_REQUIRED` — a failure without a defect reference is not accepted.
5. Retry with **Defect** `JIRA-101`. **Assert:** `Open defects` = 1.
6. Try **Close**. **Assert:** `409 TESTPLAN.OPEN_DEFECTS`.
7. Record `TC-1` again as `Passed`. **Assert:** **Retested** = yes, `Open defects` = 0, and the plan can
   now close.
8. **UAT reconciliation** for `TC-2`: **Engine outcome** `Rejected`, **Platform outcome** `Accepted`.
   **Assert:** `UAT` = `Mismatch` and `UAT mismatches` = 1 — a disagreement between engine and platform
   is surfaced rather than averaged away.

**Assert on coverage:** risk weights are `Critical 8, High 4, Medium 2, Low 1`, and only `Passed` or
`Failed` cases count as executed. Executing the `Critical` case alone gives ≈ 89% risk-weighted
coverage (8 of 9) — which is the point: exercising the risky case matters more than the case count.

**Guard rails:** activating a plan with no cases → `409 TESTPLAN.EMPTY`; duplicate case reference →
`409 TESTPLAN.DUPLICATE_CASE`; execution status `NotRun` → `400 TESTPLAN.NOT_AN_EXECUTION`.

---

## Scenario 8 — Can we sign the cutover off?

**Problem solved:** somebody has to sign, and they must see exactly what residual risk they are
accepting. This scenario proves the go/no-go recommendation is computed from evidence, and that a
signature cannot be given against a no-go.

**Steps** (admin UI → **Prove → Cutover**, plan `CUTOVER-2026`):

1. Read the go/no-go pack: **Residual exposure**, **Tolerance**, **Payments at risk**, **Open cases**,
   **Expired exceptions**, **Open defects**, **Test coverage**, **UAT mismatches**,
   **Entry outstanding**, **Exit outstanding**, **Waived** — plus "Based on run … captured …".
2. Inspect the eight seeded criteria (`ENTRY-READINESS`, `ENTRY-EXCEPTIONS`, `ENTRY-TESTING`,
   `ENTRY-SOURCES`, `ENTRY-FREEZE`, `EXIT-REJECTS`, `EXIT-RECURRENCE`, `EXIT-HYPERCARE`) with owners and
   blocking flags.
3. **Record criterion status**: `ENTRY-READINESS`, **Status** `Met`, no **Evidence**.
   **Assert:** `400 CUTOVER.EVIDENCE_REQUIRED` — "met" always needs a pointer to proof.
4. Repeat with **Evidence** `evidence://readiness/1`. Then try `Waived` on another criterion with no
   **Rationale** → `400 CUTOVER.RATIONALE_REQUIRED`.
5. **Sign off** with **Role** `Programme`, **Decision** `Approved`.
   **Assert:** while residual exposure exceeds tolerance (default tolerance `0`), you get
   `409 CUTOVER.CANNOT_APPROVE_NO_GO`.
6. Set **Operational plan**: **Freeze from** `2026-11-17`, **Freeze to** `2026-11-24`, plus fallback and
   support model → **Save**.

**Assert on the recommendation logic:**

- `NoGo` — a blocking criterion `Failed`, an approval `Rejected`, residual exposure over tolerance, or
  expired exceptions.
- `GoWithConditions` — an entry criterion still `Pending`, any criterion `Waived`, or open
  defects/UAT mismatches.
- `Go` — none of the above.

To reach a signable state, remediate until `BASE-REMEDIATED` rejects nothing, or raise
`simulation.residual-exposure-tolerance` via settings — and note that the approval records the
recommendation *as it stood at sign-off*, so a later change cannot silently re-interpret the signature.

---

## Scenario 9 — One number, many audiences

**Problem solved:** an executive, a scheme manager and an operations lead need different views of the
same truth, must be able to drill from a headline into records, and must be told when a number is
incomplete rather than being shown a confident wrong figure.

**Steps** (admin UI → **Govern → Dashboards**):

1. Cycle **Audience** through `Executive`, `Scheme`, `Source`, `Operations`, `Remediation`, `Testing`,
   `Cutover`.
2. On `Executive`, drill from **Readiness under future rules** into the `Scheme` dimension, then repeat
   for `Source`, `Country`, `Issue`.
3. Set **Schemes** `SEPA` and **Sources** `HUB-EU`, click **Refresh**. Note the `scopeKey`
   (`SEPA|HUB-EU|*|-|latest`) and `scopeDescription`.
4. Click **Export CSV**. **Assert:** the file starts with provenance headers — `# Dashboard`,
   `# Scope`, `# Captured`, `# Source as of`, `# Ruleset`, `# Reconciliation` — before
   `Section,Key,Label,Value,Unit`. An exported number always carries where and when it came from.
5. **Assert on honesty:** with no remediation data the header reads `Reconciliation: Partial` and
   `No data from remediation.` Stop the remediation container and **Refresh** to see it change; stop
   validation, remediation and simulation to get `Unreconciled`.

Snapshots are cached for `reporting.freshness-seconds` (default 300); **Refresh** forces a rebuild.

**Guard rails:** unknown audience → `400`; unknown drill dimension → `400
REPORTING.UNKNOWN_DIMENSION`; **Export CSV** as `pdr-user` → `403` (`reporting.export`).

---

## Scenario 10 — Change the rules without losing history

**Problem solved:** scheme rules change on dates set by somebody else. You need to author the next
version safely, activate it on its effective date, and roll back if it misfires — without mutating
history.

**Steps** (admin UI → **Govern → Rules**):

1. Read the **Schemes** table: `SEPA` shows structured addresses mandatory from `2026-11-15`.
2. Select the SEPA **Ruleset**, click **New draft version (copy of v1)**.
3. In **Add a rule to a draft version** enter **Code** `ADDR.TOWN_MAXLEN`, **Field** `TownName`,
   **Kind** `MaxLength`, **Parameter** `35`, **Severity** `Error`, **Applies to** `Future` →
   **Add rule**.
4. **Activate** the new version with an effective date. **Assert:** v1 becomes `Retired` with
   `effectiveTo` set to that date; v2 is `Active`.
5. Re-run Scenario 3's validation. **Assert:** the readiness numbers move — rules are versioned and
   dated, so a past number stays explainable by the ruleset that produced it.
6. Click **Roll back to this version** on v1. **Assert:** v1 is active again and v2 is retired.

**Guard rails to trip:**

| Action | Expected |
| --- | --- |
| Add a rule to the now-active version | `409 RULESET.VERSION_IMMUTABLE` |
| Activate a version with no rules | `422 RULESET.NO_RULES` |
| Add a duplicate rule code in one version | `409 RULESET.DUPLICATE_RULE_CODE` |
| `Kind` `Pattern` with an invalid regex, or `MaxLength` not a positive integer | `400 RULESET.INVALID_PARAMETER` |
| Activate as a user without `rules.activate` | `403` |

**UI trap:** `RulesAdminPage` keeps the target version in state initialised to `1` and does not update
it when you create a draft, so the selector can *display* the new draft while the POST still goes to
the old version — visible as `409 RULESET.VERSION_IMMUTABLE`. After creating a draft, explicitly
re-select the version before submitting.

---

## Scenario 11 — Prove nobody quietly edited the evidence

**Problem solved:** an audit trail that can be edited is not evidence. This scenario proves the ledger
is hash-chained and that tampering is detectable.

**Steps** (admin UI → **Govern → Audit**):

1. The ledger seeds empty. Append a few records so filters have something to distinguish:

```bash
for a in alice bob carol; do
  curl -s -o /dev/null -X POST -H "Authorization: Bearer $T" -H 'Content-Type: application/json' \
    -d "{\"service\":\"remediation\",\"action\":\"case.approved\",\"entityType\":\"Case\",\"entityId\":\"C-$a\",\"outcome\":\"Success\",\"actor\":\"$a\"}" \
    http://localhost:5100/api/v1/audit
done
```

2. Filter by **Service**, **Action**, **Actor**, **Outcome** (`Success`, `Failure`, `Denied`).
3. Click **Verify chain integrity**. **Assert:** `Intact — N records verified`.
4. Tamper behind the application's back:

```bash
docker exec pdr-postgres-1 psql -U pdr -d pdr_audit \
  -c "UPDATE audit_records SET actor='mallory' WHERE sequence=2"
```

5. Click **Verify chain integrity** again. **Assert:** `Broken at sequence 2`. Each record hashes
   `sequence|timestamp|service|action|entityType|entityId|actor|actorId|outcome|correlationId|legalEntity|details|previousHash`
   with SHA-256 and stores the predecessor's hash, so any edit fails to reproduce.
6. Restore the original actor and verify again. **Assert:** `Intact`.

**Note:** services do not currently write to this ledger automatically — it is an append API, so a full
ledger is populated by callers. Verify with `GET /api/v1/audit` returning `totalCount: 0` on a fresh
stack.

**Guard rails:** append as `pdr-user` → `403 AUTH.FORBIDDEN` (`audit.write`); verify without
`audit.verify` → `403`.

---

## Scenario 12 — Tell the right people, provably

**Problem solved:** exposure changes must reach owners and ITSM queues, deliveries must be tamper-proof
and retried, and a dead delivery must be replayable.

**Steps** (admin UI → **Govern → Notifications**):

1. **New subscription**: **Code** `OPS-INAPP`, **Event pattern** `readiness.*`, **Channel** `InApp`,
   **Target** `ops-team`, **Min severity** `Info` → **Create**.
2. Publish an event and dispatch it:

```bash
curl -s -o /dev/null -X POST -H "Authorization: Bearer $T" -H 'Content-Type: application/json' \
  -d '{"eventType":"readiness.changed","severity":"Warning","subject":"Readiness dropped to 40%","payload":"{\"futureReadinessPercent\":40}"}' \
  http://localhost:5100/api/v1/notifications/events
curl -s -X POST -H "Authorization: Bearer $T" -H 'Content-Type: application/json' -d '{}' \
  http://localhost:5100/api/v1/notifications/deliveries/dispatch
```

**Assert:** `{"attempted":1,"delivered":1,"retrying":0,"deadLettered":0}` and the **Deliveries** table
shows `Delivered`.

3. Pattern matching: `*` matches everything, `prefix.*` matches a namespace, otherwise exact match;
   severity must be at least **Min severity** (`Info` < `Warning` < `Critical`). Create a second
   subscription with **Min severity** `Critical` and re-publish the `Warning` event.
   **Assert:** it is not delivered to that subscription.
4. Webhook signing: a `Webhook` subscription must carry a signing secret, and deliveries are signed with
   headers `PDR-Signature` (`v1=<hex HMAC-SHA256>` over `{unixTimestamp}.{body}`), `PDR-Timestamp` and
   `PDR-Event-Id`. Rotate with **Rotate secret** (`notifications.admin`); the secret is never returned,
   only `Signed: yes`.
5. Retries: failed deliveries back off 1, 2, 4, 8 … minutes up to
   `notifications.max-backoff-minutes` (60), for `notifications.max-delivery-attempts` (5) attempts,
   then `DeadLettered`; 20 consecutive failures disable the subscription. Click **Replay** on a dead
   delivery. **Assert:** attempts reset and status returns to `Pending`; replaying a `Delivered` one
   gives `409 NOTIFICATION.DELIVERY_NOT_REPLAYABLE`.
6. **Scheduled reports**: create one for audience `executive`, **Frequency** `Daily`, **Hour (UTC)**
   `6`, then **Run now**. It publishes `report.executive`, so only subscriptions matching `report.*`
   or `report.executive` receive it.

**Guard rails to trip:**

| Action | Expected |
| --- | --- |
| `Webhook` subscription with no signing secret | `400 SUBSCRIPTION.SECRET_REQUIRED` |
| `Webhook` target that is neither HTTPS nor loopback | `400 SUBSCRIPTION.INSECURE_TARGET` |
| Publish a payload over 65536 bytes | `400 NOTIFICATION.PAYLOAD_TOO_LARGE` |
| Publish twice with the same `Idempotency-Key` | original notification returned, no duplicate deliveries |

**Environment note:** in the containerised stack (`ASPNETCORE_ENVIRONMENT=Production`) the loopback
exemption does not help, because containers reach each other by service name. Webhook targets must be
HTTPS there; use `InApp` to exercise the delivery pipeline.

---

## Scenario 13 — Least privilege actually holds

**Problem solved:** hiding a button is not a security control. The API must refuse, regardless of what
the UI shows.

**Steps:**

1. Sign into the admin UI as `pdr-user`. **Assert:** action buttons (Attest, Upload, Generate cases,
   Activate, Sign) are absent, because the UI calls `hasPermission()`.
2. Call the write endpoints directly with the `pdr-user` token:

```bash
U=$(token pdr-user)
curl -s -o /dev/null -w '%{http_code}\n' -X POST -H "Authorization: Bearer $U" \
  -H 'Content-Type: application/json' -d '{}' http://localhost:5100/api/v1/remediation/cases/generate
```

**Assert:** `403` with `code: AUTH.FORBIDDEN` and a ProblemDetails body — the API enforces it
independently of the UI.

3. Call any endpoint with no `Authorization` header. **Assert:** `401 AUTH.UNAUTHENTICATED`.
4. As `pdr-checker`, try to *propose* a correction. **Assert:** `403` — approving and authoring are
   deliberately different permissions.
5. Read-only users may see empty tables where a query 403s, because pages do not render `isError`
   consistently. Confirm against the network response rather than the empty table.

---

## Scenario 14 — The shell itself

**Problem solved:** an operations console is used all day; navigation, theming and deep links have to
behave.

**Steps:**

1. **Assess / Remediate / Prove / Govern / Platform** group the 14 pages along the delivery story. Click
   through each; **assert** the active item is highlighted and the top-bar breadcrumb reads
   `Group / Page`.
2. Click the panel icon in the top bar. **Assert:** the sidebar collapses to icons with tooltips, and
   the content area widens.
3. Click the moon/sun icon. **Assert:** the theme flips, and after a full page reload it *stays* — the
   choice is stored and applied before first paint, so there is no light flash on a dark reload.
4. Open `http://localhost:5174/audit` directly. **Assert:** it deep-links correctly (nginx SPA
   fallback) and the breadcrumb reads `Govern / Audit`.
5. Narrow the window below ~960px. **Assert:** the sidebar stacks above the content and the username
   collapses to just the avatar.
6. Public UI on <http://localhost:5173>: **assert** the changelog hero, the release timeline, tag
   colours per entry type (`feature`, `fix`, `security`, `change`, `deprecation`), working
   **Component**/**Per page** filters and pagination, and its own theme toggle.

---

## Suggested regression pack

For a quick confidence check after any change, run scenarios **2 → 3 → 4 → 5** in order (ingest under
`HUB-EU`, validate, four-eyes remediate, write back and roll back), then **11** (tamper the ledger and
restore) and **13** (403/401 checks). That path touches every service, both rule modes, the maker-checker
control, reversibility and the security boundary.
