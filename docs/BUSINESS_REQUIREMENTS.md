# Payment Data Readiness & Remediation Platform — Business Requirements

**Document type:** Business Requirements Document (BRD)  
**Target state:** Production enterprise platform, not the public demonstration  
**Status:** Baseline for discovery, scheme/legal validation, estimation, and phased delivery  
**Version:** 1.0 — 3 September 2026

## 1. Executive summary

The Payment Data Readiness & Remediation Platform helps banks identify, correct, test, and evidence payment-party address data that will fail current or future payment-scheme validation. Its first regulatory use case is the end of unstructured postal addresses for affected EPC payment-scheme transactions, but the production product must provide a configurable payment-data quality and cutover-control capability rather than a one-date utility.

The platform must discover affected data across payment channels, standing orders, mandates, corporate files, core systems, master/customer/counterparty data, and archives; validate ISO 20022 messages and source records; manage human-controlled remediation; coordinate source-system fixes; simulate future rules; support UAT/cutover; and provide defensible readiness evidence.

## 2. Problems to solve

1. Postal-address data is duplicated across customer, counterparty, mandate, standing-order, channel, ERP, file, and payment-processing systems.
2. Banks cannot quantify which future-dated or recurring transactions will fail after a scheme-rule change.
3. Current files can contain unstructured, hybrid, incomplete, conflicting, invalid-country, or low-confidence address data.
4. Manual spreadsheet remediation has weak ownership, duplicate effort, no controlled approval, and poor auditability.
5. An apparently valid payment message may be generated from an unremediated upstream source and fail again later.
6. Scheme rules, dates, message versions, country/address expectations, and market-infrastructure positions can change.
7. Bulk automatic correction creates customer, fraud, sanctions, privacy, and operational risk when evidence is insufficient.
8. Testing and cutover teams need repeatable simulations, source-by-source exposure, and traceable go/no-go evidence.

## 3. Current regulatory baseline

As of this document date, EPC guidance and the applicable 2025 rulebook updates identify **15 November 2026** as the date from which unstructured addresses are no longer permitted for the affected EPC schemes. The EPC's 27 August 2026 communication stated that its timeline remained unchanged until further notice and that it would determine its position at the Payment Scheme Management Board meeting on 9 September 2026.

The production platform must therefore:

- keep dates, schemes, versions, countries, formats, validation rules, and effective periods configurable;
- preserve the source and approval of every ruleset;
- monitor and assess rule changes before activation;
- run current-state and future-state rules in parallel;
- avoid presenting a platform rule as legal/scheme truth without approved governance.

## 4. Product vision

Create a reusable payment-data control plane that tells the bank what will fail, why it will fail, where the defective source is, who must fix it, what evidence supports a correction, and whether the bank is ready before a rule or format cutover.

## 5. Business objectives and success measures

| Objective | Target measure after rollout |
|---|---|
| Establish exposure | 100% of in-scope channels/sources assessed against approved current and future rules |
| Prevent rejects | At least 99.5% projected compliance before production cutover, with accepted exceptions documented |
| Fix root sources | At least 95% of approved corrections written back to or resolved in the authoritative source |
| Control remediation | 100% of low-confidence/material address changes independently verified before application |
| Protect service | No material increase in fraud, sanctions, repair, return, reject, or complaint rates caused by remediation |
| Prove readiness | Complete, reproducible go/no-go evidence by scheme, source, product, customer segment, and country |
| Sustain quality | Post-cutover defect rate and recurrence remain within institution-approved thresholds |

## 6. Stakeholders and personas

| Persona | Primary need |
|---|---|
| Payments product/scheme owner | Interpret scope, own readiness, approve rules and go/no-go |
| Payment operations | Find rejects/repair risk, review cases, coordinate corrections and exceptions |
| Data steward/customer or counterparty owner | Verify proposed structured addresses against authoritative evidence |
| Channel/source-system owner | Correct mappings, files, forms, and upstream persistence |
| Corporate servicing team | Coordinate client file/ERP changes and track client readiness |
| Compliance/sanctions/financial crime | Ensure changes do not weaken screening or create party ambiguity |
| Privacy/legal | Confirm lawful processing, notice, retention, and external-data use |
| Engineering/integration | Connect sources, implement validation, remediation write-back, and testing |
| QA/release/cutover manager | Execute future-rule tests, reconcile results, and support go/no-go |
| Risk/internal audit | Review governance, overrides, evidence, residual exposure, and controls |
| Executive steering committee | See readiness, risk, blockers, and owner accountability |

## 7. Business scope

### 7.1 In scope

- Configurable scheme/rule/version/effective-date knowledge base and change governance.
- Discovery and inventory of all in-scope payment/address sources and data lineage.
- Batch, streaming, API, file, and database ingestion using approved access patterns.
- Parsing and validation of ISO 20022 XML and bank-approved CSV/delimited extracts.
- Current/future validation, profiling, issue classification, deduplication, and exposure projection.
- Address normalization and proposal generation using approved internal/external reference sources.
- Human review, source-owner confirmation, maker-checker, exception, and evidence workflows.
- Controlled write-back/export to authoritative sources with reconciliation and rollback.
- Campaign management for internal teams and corporate customers.
- Scenario simulation, UAT, regression, volume/performance testing, cutover, and hypercare.
- Readiness dashboards, scheduled reports, issue/action management, audit, and evidence packs.
- Ongoing post-cutover data-quality monitoring and reusable rules for future payment-data changes.

### 7.2 Out of scope for the initial production release

- Replacing payment engines, customer/master-data platforms, sanctions screening, or corporate ERP products.
- Silent correction based only on generative AI or an unverified external address service.
- Automatically changing customer/counterparty legal identity or contact data without source-system authority.
- Guaranteeing scheme acceptance where other message or business validation can still fail.

## 8. Required business capabilities

1. **Rule governance:** effective-dated rules by scheme, message, role, geography, format, and source.
2. **Source inventory and lineage:** connect the payment instruction to the authoritative customer/counterparty/address source and generator.
3. **Data discovery and profiling:** measure volumes, formats, completeness, defects, recurrence, and future-dated exposure.
4. **Validation:** explain exact rule failures at message, party, address, field, source, and batch level.
5. **Remediation:** generate proposals, verify evidence, manage uncertainty, approve, write back, and reconcile.
6. **Campaign/ownership:** route work to internal source owners or corporate customers with SLA/escalation.
7. **Simulation and testing:** replay representative populations against current/future rules and approved remediation.
8. **Cutover control:** manage entry/exit criteria, freezes, deployment/mapping readiness, fallback, and go/no-go evidence.
9. **Post-cutover monitoring:** identify rejects, repairs, regressions, recurring defects, and source leakage.
10. **Reporting and evidence:** provide accurate exposure, progress, residual risk, decision, and audit reporting.

## 9. Core business processes

### 9.1 Rule change intake

The scheme owner records a regulatory/scheme source, interpretation, affected schemes/messages/roles, proposed rules, effective date, and transition behavior. Legal/compliance/operations/technology reviewers approve the version. The platform simulates impact before activation and retains prior versions.

### 9.2 Discover and assess

Source owners register systems and data lineage. Scheduled scans ingest or process authorized data, classify address formats, validate fields, group defects, identify recurrence/future-dated exposure, and publish a reconciled readiness baseline.

### 9.3 Remediate

The platform links defects to the authoritative party/address, proposes structured fields with confidence and evidence, and routes work by policy. A reviewer approves, edits, requests source/customer input, rejects, or records an exception. Approved corrections are written back/exported, then read-after-write reconciliation confirms the outcome.

### 9.4 Test and cut over

QA selects representative and worst-case populations, runs future rules and end-to-end payment processing in a controlled environment, reconciles results, tracks defects, and records entry/exit criteria. The steering authority receives residual exposure, unresolved exceptions, rollback/fallback, and operational readiness for go/no-go.

### 9.5 Hypercare and ongoing control

Production rejects/repairs and upstream format leakage are monitored by scheme/source. Recurrences reopen source remediation. Rule updates follow the same governed lifecycle.

## 10. Business rules

1. Each validation result must identify the ruleset/version/effective date and exact source data assessed.
2. The platform must distinguish message-level compliance from authoritative-source remediation.
3. An address proposal must show original value, parsed/normalized fields, source/evidence, confidence, and unresolved ambiguity.
4. Low-confidence, identity-affecting, sanctions-relevant, cross-border, or policy-defined material changes require maker-checker or source-owner confirmation.
5. Original data and decision history must remain recoverable under retention policy; approved write-back must be idempotent and reconcilable.
6. A record cannot be counted as remediated until the designated authoritative target and regenerated payment output pass the approved rule.
7. Scheme dates/rules must not change automatically from external content; an authorized owner must review and activate them.
8. Counts and rates must reconcile from record to batch, source, scheme, and portfolio with a declared as-of time.
9. Production source access is read-only by default; write-back requires explicit source, field, role, approval, and rollback controls.
10. Personal data must be minimized/masked in non-production and exports according to policy.

## 11. Security, privacy, and risk outcomes

- SSO/MFA, least privilege, maker-checker, segregation of duties, privileged-access control, and service-account scoping.
- Encryption, secrets/key management, data residency, masked non-production data, export control, and secure deletion.
- Approved lawful purpose and retention for address processing; controlled use of external address/reference services.
- No production payment/customer data used to train AI models; deterministic validation remains authoritative.
- Malware-safe file handling, schema/entity limits, parser hardening, quarantine, and denial-of-service protection.
- Full audit of ingestion, validation, proposal, view of restricted data, decision, write-back, export, rule, and access changes.

## 12. Risks and mitigations

| Risk | Required mitigation |
|---|---|
| Scheme position changes | Versioned effective-dated rules, watch process, impact simulation, human activation |
| Correction changes the wrong party | Authoritative identifiers, evidence, duplicate detection, maker-checker, rollback |
| Dashboard counts are misleading | Reconciliation controls, as-of date, exclusions, population definition, lineage |
| File/parser exploit or data leak | Isolation, scanning, size/schema limits, encryption, masking, least privilege |
| Temporary message repair hides bad source | Source lineage, recurrence detection, write-back confirmation, regenerated-message test |
| Corporate customers are late | Campaign tracking, templates, validation feedback, escalation, controlled exceptions |
| Cutover disrupts payments | Parallel testing, staged rollout, fallback, monitoring, hypercare, go/no-go governance |

## 13. Business acceptance criteria

1. Every in-scope scheme/source has an approved population definition, owner, lineage, scan, exposure, and action plan.
2. Validation produces reproducible field-level results against current and future approved rules.
3. A correction can be proposed, reviewed, approved, written back/exported, reconciled, regenerated, retested, and audited end to end.
4. Cutover simulation covers future-dated and recurring instructions, affected schemes, source systems, countries, corporate files, and defined exception populations.
5. Go/no-go reporting reconciles totals and shows residual risk, owners, exceptions, tests, operational readiness, fallback, and approvals.
6. Security, privacy, performance, recovery, parser, access, and audit controls pass institution testing.

## 14. Authoritative references

- [EPC — November 2026 end-date communication](https://www.europeanpaymentscouncil.eu/news-insights/news/november-2026-end-date-unstructured-address-format-epc-payment-scheme)
- [EPC — Guidance on provision of addresses under EPC payment schemes](https://www.europeanpaymentscouncil.eu/document-library/guidance-documents/epc-guidance-document-provision-addresses-under-epc-payment)
- [EPC — 2025 SCT rulebook version 1.1](https://www.europeanpaymentscouncil.eu/document-library/rulebooks/2025-sepa-credit-transfer-rulebook-version-11)
- [EPC — 2025 SDD Core rulebook version 1.1](https://www.europeanpaymentscouncil.eu/document-library/rulebooks/2025-sepa-direct-debit-core-rulebook-version-11)

Institution-approved scheme interpretation remains mandatory.

