# Payment Data Readiness & Remediation — Functional Requirements and Implementation Backlog

**Target state:** Enterprise production platform  
**Related document:** [Business Requirements](./BUSINESS_REQUIREMENTS.md)  
**Version:** 1.0 — 3 September 2026

## 1. System context

The production system includes a secured web application, versioned API, effective-dated rules service, ingestion/parsing workers, deterministic validation engine, remediation/workflow service, operational database, protected evidence/object storage, analytics/reporting store, audit ledger, scheduler, notifications, and source adapters. Authoritative customer, counterparty, mandate, standing-order, channel, ERP, and payment systems remain systems of record.

## 2. Roles

`Platform Administrator`, `Scheme/Rule Owner`, `Payments Product Owner`, `Source-System Owner`, `Data Steward`, `Maker`, `Checker/Approver`, `Corporate Servicing User`, `Operations User`, `Compliance/Financial-Crime Reviewer`, `Privacy/Legal Reviewer`, `QA/Test Manager`, `Cutover Manager`, `Risk/Auditor Read-Only`, and scoped `Integration Service Account`.

## 3. Functional requirements

### 3.1 Administration and access

- **FR-ADM-001:** Authenticate through enterprise OIDC/SAML SSO and enforce the institution's MFA/session policies.
- **FR-ADM-002:** Support legal entities, countries, business units, schemes, environments, data domains, and delegated administration.
- **FR-ADM-003:** Enforce RBAC/ABAC and maker-checker rules on records, fields, sources, files, actions, exports, and APIs.
- **FR-ADM-004:** Configure data classification, masking, retention, residency, export, notification, SLA, and escalation policies.
- **FR-ADM-005:** Manage scoped connector credentials in an approved secrets service.

### 3.2 Rule and reference-data governance

- **FR-RULE-001:** Register source documents, interpretation, owner, approvers, affected schemes/messages/party roles, validation rules, severity, effective period, and version.
- **FR-RULE-002:** Support SCT, SCT Inst, SDD Core, SDD B2B, OCT Inst, and extensible institution-defined schemes.
- **FR-RULE-003:** Model current, transition, and future rule states without overwriting history.
- **FR-RULE-004:** Represent structured, hybrid, and unstructured address requirements; mandatory/conditional fields; country code; character/length; occurrence; and cross-field rules.
- **FR-RULE-005:** Import controlled ISO country/postal/reference data with provenance, version, effective date, and approval.
- **FR-RULE-006:** Test a draft ruleset against a representative baseline and compare result deltas before approval.
- **FR-RULE-007:** Require authorized review and activation; record rationale and change notes; support rollback to a prior active configuration.
- **FR-RULE-008:** Notify impacted owners of published changes and automatically create reassessment tasks.

### 3.3 Source inventory and lineage

- **FR-SRC-001:** Register source systems, owners, interfaces, schedules, volumes, schemes, party roles, data classification, environments, and authoritative attributes.
- **FR-SRC-002:** Map the flow from authoritative party/address through channel/file/message generation to payment engine and external submission.
- **FR-SRC-003:** Maintain field-level mapping from source attributes to ISO 20022 address elements.
- **FR-SRC-004:** Identify recurring/future-dated instructions, templates, mandates, standing orders, beneficiaries, counterparties, and generated transactions.
- **FR-SRC-005:** Track source onboarding, scan coverage, data freshness, mapping readiness, test status, and remediation owner.
- **FR-SRC-006:** Require periodic owner attestation and escalate stale critical mappings.

### 3.4 Ingestion and file handling

- **FR-ING-001:** Ingest through approved APIs, database views, events, secure file transfer, object storage, or interactive upload.
- **FR-ING-002:** Support ISO 20022 XML, including configured pain/pacs message versions, and approved CSV/delimited layouts.
- **FR-ING-003:** Validate file type, encoding, size, schema, entity expansion limits, record count, checksum, and malware status before processing.
- **FR-ING-004:** Quarantine unsafe/invalid inputs and provide non-sensitive error details.
- **FR-ING-005:** Process large files asynchronously with progress, cancellation policy, checkpoint, retry, and idempotency.
- **FR-ING-006:** Record source, batch, file, checksum, rule version, timestamps, counts, exclusions, and parser version.
- **FR-ING-007:** Prevent duplicate scans or clearly mark intentional reprocessing.
- **FR-ING-008:** Support streaming/pre-submission validation API with strict latency and availability targets.

### 3.5 Parsing, profiling, and validation

- **FR-VAL-001:** Extract message, payment, party, account-reference, address, scheme, date, source, and batch context without unnecessary data persistence.
- **FR-VAL-002:** Classify each address as structured, hybrid, unstructured, absent, or unrecognized with explanation.
- **FR-VAL-003:** Evaluate current and selected future rules deterministically and return rule ID, field, severity, expected value/format, actual state, and evidence pointer.
- **FR-VAL-004:** Detect missing/invalid country, town, postcode, street, building number, address line, duplicates, conflicting fields, and country-specific anomalies when configured.
- **FR-VAL-005:** Profile counts/rates by scheme, message, party role, source, channel, product, country, segment, date, format, and issue.
- **FR-VAL-006:** Identify recurring defects and link generated payments to the probable authoritative source defect.
- **FR-VAL-007:** Distinguish rejected, warning, informational, excluded, and unable-to-assess results.
- **FR-VAL-008:** Reconcile input, parsed, valid, invalid, excluded, failed, and duplicate counts at batch and portfolio levels.
- **FR-VAL-009:** Support sampling and authorized drill-down from aggregate to masked/full record according to permissions.

### 3.6 Remediation proposal

- **FR-REM-001:** Create a remediation case per authoritative party/address issue, deduplicating repeated payment occurrences.
- **FR-REM-002:** Display original source values, message values, proposed structured fields, issue/rule, occurrences, affected schemes, future exposure, source, and owner.
- **FR-REM-003:** Generate proposals using deterministic parsing, approved reference data, verified source attributes, and optional approved address services.
- **FR-REM-004:** Record proposal method, evidence, confidence by field and overall, unresolved ambiguity, normalization, and alternative candidates.
- **FR-REM-005:** Never use generative-AI output as authoritative validation; label AI assistance and require verification.
- **FR-REM-006:** Group/prioritize cases by reject volume, future date, scheme criticality, customer impact, confidence, recurrence, and SLA.
- **FR-REM-007:** Support bulk action only for policy-eligible populations and show preview/count/rollback scope.

### 3.7 Review and workflow

- **FR-WF-001:** Route cases by source, customer segment, country, confidence, materiality, financial-crime/privacy condition, and workload.
- **FR-WF-002:** Let a maker edit proposed fields, attach/reference evidence, comment, request source/customer input, and submit.
- **FR-WF-003:** Let an independent checker approve, return, reject, dismiss, or create a time-bound exception with rationale.
- **FR-WF-004:** Enforce field-specific evidence and maker-checker/authority thresholds.
- **FR-WF-005:** Track status, owner, queue, due date, SLA, blockers, aging, communication, decision, and complete history.
- **FR-WF-006:** Support campaign assignment to internal teams and corporate customers via secure, scoped workflow or export/import.
- **FR-WF-007:** Escalate overdue/high-risk work and prevent an exception from silently being counted as compliant.

### 3.8 Write-back, export, and reconciliation

- **FR-WB-001:** Configure authorized target sources/fields, API/file format, approval, maintenance window, rate limit, and rollback method.
- **FR-WB-002:** Preview changes and reject stale updates using source version/checksum/last-modified controls.
- **FR-WB-003:** Apply approved corrections idempotently with per-record result and correlation ID.
- **FR-WB-004:** Support controlled export for sources without write APIs, including checksum, encryption, handoff, and import confirmation.
- **FR-WB-005:** Read after write or re-import to confirm the authoritative source matches the approved correction.
- **FR-WB-006:** Regenerate/replay the downstream message and revalidate it before marking the case remediated.
- **FR-WB-007:** Provide authorized rollback/reversal where supported and preserve both original and corrected values.
- **FR-WB-008:** Create incidents/tasks for partial failure and reconcile retries without duplicate writes.

### 3.9 Scenario, testing, and cutover

- **FR-SIM-001:** Define scenarios by ruleset/effective date, scheme, message, party role, source, country, segment, date range, and remediation state.
- **FR-SIM-002:** Project accepted, rejected, warning, excluded, and unable-to-assess volumes with reconciled population/exclusion definitions.
- **FR-SIM-003:** Compare current data, approved remediation, assumed source fixes, and alternative rule/date versions.
- **FR-SIM-004:** Save/re-run/version scenarios and compare results over time.
- **FR-TEST-001:** Manage test plans, representative/worst-case samples, environments, expected results, executions, evidence, defects, and retests.
- **FR-TEST-002:** Cover future-dated payments, recurring instructions, all affected schemes/roles, country patterns, corporate files, rejects, and operational repair.
- **FR-TEST-003:** Reconcile platform validation with end-to-end payment-engine/network test results.
- **FR-CUT-001:** Manage cutover milestones, entry/exit criteria, source changes, mapping deployments, data freezes, bulk fixes, fallback, contacts, and approvals.
- **FR-CUT-002:** Produce go/no-go evidence with readiness, residual exposure, exceptions, tests, incidents, capacity, support, and owner sign-off.
- **FR-CUT-003:** Support phased rollout and hypercare dashboards by source/scheme.

### 3.10 Reporting, notifications, and audit

- **FR-REP-001:** Provide dashboards for coverage, readiness, formats, issues, reject exposure, remediation funnel, SLA, source root cause, corporate readiness, tests, and cutover.
- **FR-REP-002:** Make every metric drillable/reconcilable and display as-of time, ruleset, scope, exclusions, and data freshness.
- **FR-REP-003:** Generate scheduled management, scheme-owner, operations, source-owner, corporate campaign, risk, and audit reports.
- **FR-NOT-001:** Notify users about assignments, requests, overdue work, rule changes, connector failures, threshold breaches, write-back failure, and cutover decisions.
- **FR-AUD-001:** Audit data access, scans, rule/config changes, proposals, edits, evidence, decisions, bulk actions, write-back, rollback, exports, and API calls.
- **FR-AUD-002:** Preserve actor/service, timestamp, before/after values, source, rule version, reason, approval, batch/case, and correlation IDs.
- **FR-AUD-003:** Support retention, legal hold, tamper-evidence, and authorized evidence-pack export.

### 3.11 APIs and integration

- **FR-API-001:** Expose versioned APIs for validation, batches, results, cases, workflow, rules metadata, scenarios, and reports.
- **FR-API-002:** Provide scoped service accounts, OAuth/mTLS as required, idempotency, pagination, rate limits, webhooks, and replay protection.
- **FR-API-003:** Integrate with customer/counterparty master data, payments hub, channels, mandate/standing-order systems, corporate file services, ITSM, test management, BI, and notifications.

## 4. Core data entities

`LegalEntity`, `Scheme`, `RuleSource`, `Ruleset`, `Rule`, `ReferenceDataset`, `SourceSystem`, `SourceFieldMapping`, `LineagePath`, `IngestionBatch`, `InputArtifact`, `PaymentRecord`, `Party`, `Address`, `ValidationResult`, `Issue`, `RemediationCase`, `Proposal`, `Evidence`, `Decision`, `Exception`, `WriteBackJob`, `Reconciliation`, `Campaign`, `Scenario`, `SimulationResult`, `TestPlan`, `TestExecution`, `Defect`, `CutoverPlan`, `Approval`, `Notification`, and `AuditEvent`.

## 5. Non-functional requirements

- **NFR-001 Availability:** 99.9% platform target; pre-submission validation target agreed with payment criticality and fail-open/fail-closed policy.
- **NFR-002 Recovery:** approved RTO/RPO, encrypted backups, tested restore, and documented DR/fallback.
- **NFR-003 Performance:** scalable batch processing for agreed peak files/records; p95 interactive queries under 2 seconds; validation API latency tested to contract.
- **NFR-004 Security:** secure SDLC, hardened XML/CSV parsing, malware isolation, least privilege, encryption, secrets rotation, penetration testing, and continuous vulnerability management.
- **NFR-005 Privacy:** minimization, masking/tokenization, residency, retention/deletion, restricted exports, non-production protection, and auditable access.
- **NFR-006 Integrity:** deterministic/versioned results, checksums, reconciled counts, idempotent writes, transaction boundaries, and no silent partial success.
- **NFR-007 Observability:** metrics/logs/traces, batch/job state, connector health, rule version, correlation IDs, SLOs, alerts, and runbooks.
- **NFR-008 Scale:** horizontal workers, back-pressure, queue isolation, safe retry, throttling, and capacity forecasting.
- **NFR-009 Accessibility:** WCAG 2.2 AA and accessible reports.
- **NFR-010 Maintainability:** modular rules/connectors, automated regression packs, schema/version compatibility, feature flags, and rollback.

## 6. Implementation backlog

Priority: **P0** cutover/production foundation, **P1** expansion, **P2** optimization.

### Epic PDR-01 — Foundation and rule governance

- **PDR-001 (P0):** Implement SSO, RBAC/ABAC, maker-checker, entity/scheme/source scopes, and service accounts.
- **PDR-002 (P0):** Build effective-dated rule/source/reference management with approval and immutable history.
- **PDR-003 (P0):** Encode approved structured/hybrid/unstructured address rules and regression examples for each in-scope scheme/message/role.
- **PDR-004 (P0):** Add draft-rule impact comparison, activation, notification, and rollback.
- **PDR-005 (P0):** Implement audit ledger, retention, legal hold, and protected evidence storage.

### Epic PDR-02 — Sources, lineage, and ingestion

- **PDR-006 (P0):** Register source owners, populations, interfaces, schedules, mappings, authoritative attributes, and readiness.
- **PDR-007 (P0):** Model source-to-message field lineage for channels, master data, mandates, standing orders, ERP/files, and payment hub.
- **PDR-008 (P0):** Build secure file/API/database ingestion framework with checksums, quarantine, idempotency, checkpoints, and monitoring.
- **PDR-009 (P0):** Implement hardened ISO 20022 XML parsers for approved pain/pacs versions.
- **PDR-010 (P0):** Implement configurable CSV/delimited parsers and layout validation.
- **PDR-011 (P1):** Add low-latency pre-submission validation API and operational contract.

### Epic PDR-03 — Validation and exposure

- **PDR-012 (P0):** Classify address format and produce field/rule-level current/future results.
- **PDR-013 (P0):** Detect missing/invalid/conflicting fields and country/reference anomalies.
- **PDR-014 (P0):** Reconcile batch counts and profile by scheme, source, role, country, date, segment, format, and issue.
- **PDR-015 (P0):** Detect recurring/future-dated exposure and link repeated occurrences to root source records.
- **PDR-016 (P0):** Build permission-aware drill-down, masking, sampling, and export.
- **PDR-017 (P1):** Baseline and trend readiness with freshness and scan-coverage controls.

### Epic PDR-04 — Remediation workflow

- **PDR-018 (P0):** Create deduplicated source-level remediation cases with priority and projected impact.
- **PDR-019 (P0):** Generate explainable structured-address proposals from deterministic parsing/reference/source data.
- **PDR-020 (P0):** Show per-field confidence, evidence, ambiguity, alternatives, and original/message values.
- **PDR-021 (P0):** Implement maker edit, owner/customer input, checker approval/return/reject/dismiss/exception.
- **PDR-022 (P0):** Enforce evidence, materiality, financial-crime/privacy, and bulk-action policies.
- **PDR-023 (P1):** Build workload routing, campaign assignment, SLA, reminders, and escalation.
- **PDR-024 (P1):** Integrate an approved address service behind privacy, region, confidence, and contract controls.

### Epic PDR-05 — Write-back and confirmation

- **PDR-025 (P0):** Configure target systems/fields, maintenance controls, preview, approval, and source-version conflict checks.
- **PDR-026 (P0):** Implement idempotent write-back with per-record results, retry, partial-failure handling, and correlation.
- **PDR-027 (P0):** Implement encrypted controlled export/import confirmation for non-API sources.
- **PDR-028 (P0):** Read after write, regenerate/replay message, revalidate, and only then mark remediated.
- **PDR-029 (P0):** Implement rollback/reversal policy and full before/after history.

### Epic PDR-06 — Simulation, testing, and cutover

- **PDR-030 (P0):** Build configurable current/future/remediated scenarios with reconciled populations.
- **PDR-031 (P0):** Save, compare, export, and reproduce simulation results by ruleset/scope.
- **PDR-032 (P0):** Manage risk-based test plans, samples, executions, expected results, defects, retests, and evidence.
- **PDR-033 (P0):** Reconcile platform validation to payment-engine/network UAT outcomes.
- **PDR-034 (P0):** Manage cutover entry/exit criteria, source deployments, bulk fixes, freeze, fallback, support, and approvals.
- **PDR-035 (P0):** Generate go/no-go pack with residual exposure, exceptions, testing, operational readiness, and sign-off.
- **PDR-036 (P1):** Add phased rollout and hypercare monitoring of rejects, repairs, recurrence, and source leakage.

### Epic PDR-07 — Reporting and integration

- **PDR-037 (P0):** Deliver executive, scheme, source, operations, campaign, remediation, testing, and cutover dashboards.
- **PDR-038 (P0):** Make metrics drillable with scope, exclusions, freshness, ruleset, and reconciliation controls.
- **PDR-039 (P1):** Implement scheduled reports/notifications and collaboration/ITSM task integration.
- **PDR-040 (P1):** Provide versioned APIs/webhooks with scopes, idempotency, limits, and developer documentation.
- **PDR-041 (P1):** Add reusable rule templates for future payment-data quality changes.

### Epic PDR-08 — Enterprise hardening

- **PDR-042 (P0):** Threat-model file/API/database flows and complete parser, malware, authorization, DLP, and penetration tests.
- **PDR-043 (P0):** Implement encrypted backup/restore, DR, secrets/key rotation, and recovery exercises.
- **PDR-044 (P0):** Add service/connector/job observability, SLOs, alerts, correlation, and runbooks.
- **PDR-045 (P0):** Load-test peak batches, concurrent scans, dashboards, write-back throttles, and validation API.
- **PDR-046 (P0):** Complete privacy assessment, accessibility audit, data-migration rehearsal, and operational handover.

## 7. Definition of done

Each story requires approved acceptance criteria, deterministic regression tests, authorization/audit tests, reconciled counts, negative/error-path tests, security/privacy review, observability/runbooks, rollback where data can change, accessibility validation, and owner acceptance. Rule changes additionally require scheme/legal/compliance approval and test evidence.

## 8. Recommended delivery sequence

1. PDR-001–010, PDR-012–023, PDR-030–035, PDR-037–038, PDR-042–046.
2. PDR-025–029 only after source owners approve write-back controls and a dry-run reconciliation succeeds.
3. PDR-011, PDR-024, PDR-036, PDR-039–041 after initial cutover scope is stable.

