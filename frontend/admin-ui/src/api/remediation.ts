import { apiGet, apiPost, apiPut } from './client';
import type { PartyRole } from './ingestion';
import type { PagedResult } from './releases';

export type CaseStatus =
  | 'New'
  | 'InProgress'
  | 'PendingApproval'
  | 'Approved'
  | 'Returned'
  | 'Rejected'
  | 'Dismissed'
  | 'ExceptionGranted'
  | 'WriteBackPending'
  | 'Remediated'
  | 'Failed'
  | 'RolledBack';

export type CasePriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type DecisionType = 'Approve' | 'Return' | 'Reject' | 'Dismiss' | 'GrantException';
export type ProposalMethod =
  | 'DeterministicParse'
  | 'ReferenceData'
  | 'SourceAttribute'
  | 'ManualEdit'
  | 'AssistedSuggestion';
export type WriteBackMode = 'Api' | 'Export';
export type WriteBackStatus = 'Pending' | 'Applied' | 'Confirmed' | 'PartiallyFailed' | 'Failed' | 'RolledBack';
export type WriteBackItemStatus = 'Pending' | 'Applied' | 'Confirmed' | 'Failed' | 'Stale' | 'RolledBack';

export const caseStatuses: CaseStatus[] = [
  'New',
  'InProgress',
  'PendingApproval',
  'Approved',
  'Returned',
  'Rejected',
  'Dismissed',
  'ExceptionGranted',
  'WriteBackPending',
  'Remediated',
  'Failed',
  'RolledBack',
];

export const casePriorities: CasePriority[] = ['Low', 'Medium', 'High', 'Critical'];

export interface CaseListItemDto {
  id: string;
  caseKey: string;
  sourceCode: string;
  partyName?: string | null;
  partyRole: PartyRole;
  country?: string | null;
  issueRuleCodes: string;
  affectedSchemes: string;
  occurrences: number;
  futureExposure: number;
  priority: CasePriority;
  priorityScore: number;
  status: CaseStatus;
  queue?: string | null;
  assignedTo?: string | null;
  dueDate?: string | null;
  isOverdue: boolean;
  confidence?: number | null;
  campaignId?: string | null;
  openedAtUtc: string;
}

export interface ProposalDto {
  id: string;
  method: ProposalMethod;
  requiresHumanVerification: boolean;
  country?: string | null;
  townName?: string | null;
  postCode?: string | null;
  streetName?: string | null;
  buildingNumber?: string | null;
  countryConfidence: number;
  townConfidence: number;
  postCodeConfidence: number;
  streetConfidence: number;
  buildingNumberConfidence: number;
  overallConfidence: number;
  ambiguity?: string | null;
  alternatives?: string | null;
  notes?: string | null;
}

export interface OriginalAddressDto {
  country?: string | null;
  townName?: string | null;
  postCode?: string | null;
  streetName?: string | null;
  buildingNumber?: string | null;
  addressLines?: string | null;
}

export interface CaseEvidenceDto {
  id: string;
  kind: string;
  reference: string;
  description?: string | null;
  capturedBy: string;
  capturedAtUtc: string;
}

export interface CaseEventDto {
  id: string;
  action: string;
  fromStatus: CaseStatus;
  toStatus: CaseStatus;
  actor: string;
  rationale?: string | null;
  occurredAtUtc: string;
}

export interface CaseDetailDto extends CaseListItemDto {
  ownerName?: string | null;
  ownerEmail?: string | null;
  original: OriginalAddressDto;
  proposal?: ProposalDto | null;
  evidencePointer: string;
  submittedBy?: string | null;
  submittedAtUtc?: string | null;
  decidedBy?: string | null;
  decidedAtUtc?: string | null;
  decisionRationale?: string | null;
  exceptionExpiresOn?: string | null;
  isExceptionExpired: boolean;
  failureReason?: string | null;
  remediatedAtUtc?: string | null;
  evidence: CaseEvidenceDto[];
  history: CaseEventDto[];
}

export interface CaseGenerationDto {
  runId: string;
  assessmentsRead: number;
  casesCreated: number;
  casesUpdated: number;
  occurrencesFolded: number;
  generatedAtUtc: string;
}

export interface FunnelBucketDto {
  key: string;
  caseCount: number;
  openCount: number;
  futureExposure: number;
}

export interface RemediationFunnelDto {
  totalCases: number;
  openCases: number;
  pendingApproval: number;
  approved: number;
  remediated: number;
  dismissed: number;
  rejected: number;
  exceptionsGranted: number;
  expiredExceptions: number;
  overdue: number;
  futureExposureOpen: number;
  futureExposureRemediated: number;
  remediationPercent: number;
  byPriority: FunnelBucketDto[];
  bySource: FunnelBucketDto[];
  asOfUtc: string;
}

export interface BulkPreviewDto {
  action: string;
  matchedCases: number;
  eligibleCases: number;
  blockedCases: number;
  futureExposure: number;
  lowestConfidence?: number | null;
  rollbackSupported: boolean;
  blockedReasons: string[];
  sample: CaseListItemDto[];
}

export interface BulkResultDto {
  action: string;
  applied: number;
  skipped: number;
  failures: string[];
}

export interface WriteBackTargetDto {
  id: string;
  sourceCode: string;
  mode: WriteBackMode;
  writableFields: string;
  endpoint?: string | null;
  exportFormat?: string | null;
  maintenanceWindow?: string | null;
  maxRecordsPerRun: number;
  requiresApproval: boolean;
  rollbackMethod: string;
  isEnabled: boolean;
}

export interface WriteBackChangeDto {
  caseId: string;
  recordReference: string;
  field: string;
  beforeValue?: string | null;
  afterValue?: string | null;
}

export interface WriteBackPreviewDto {
  targetSourceCode: string;
  mode: WriteBackMode;
  maintenanceWindow?: string | null;
  maxRecordsPerRun: number;
  rollbackMethod: string;
  eligibleCases: number;
  recordsToWrite: number;
  changes: WriteBackChangeDto[];
  blockers: string[];
}

export interface WriteBackItemDto {
  id: string;
  caseId: string;
  recordReference: string;
  sourceVersion?: string | null;
  beforeValue: string;
  afterValue: string;
  status: WriteBackItemStatus;
  message?: string | null;
  correlationId?: string | null;
  appliedAtUtc?: string | null;
}

export interface WriteBackJobDto {
  id: string;
  targetSourceCode: string;
  mode: WriteBackMode;
  status: WriteBackStatus;
  idempotencyKey: string;
  requestedBy: string;
  requestedAtUtc: string;
  appliedAtUtc?: string | null;
  confirmedAtUtc?: string | null;
  failureSummary?: string | null;
  exportChecksum?: string | null;
  itemCount: number;
  appliedCount: number;
  confirmedCount: number;
  failedCount: number;
  staleCount: number;
  rolledBackCount: number;
  countsReconcile: boolean;
  items: WriteBackItemDto[];
}

export interface WriteBackReconciliationDto {
  jobId: string;
  requested: number;
  applied: number;
  confirmed: number;
  failed: number;
  stale: number;
  rolledBack: number;
  balanced: boolean;
  discrepancies: string[];
}

export interface CaseFilter {
  status?: CaseStatus | '';
  priority?: CasePriority | '';
  sourceCode?: string;
  queue?: string;
  ruleCode?: string;
  overdueOnly?: boolean;
}

export interface BulkSelection {
  sourceCode?: string;
  queue?: string;
  ruleCode?: string;
  status?: CaseStatus;
  minimumPriority?: CasePriority;
  minimumConfidence?: number;
  caseIds?: string[];
}

export const getCases = (page: number, filter: CaseFilter) => {
  const query = new URLSearchParams({ page: String(page) });
  if (filter.status) query.set('status', filter.status);
  if (filter.priority) query.set('priority', filter.priority);
  if (filter.sourceCode) query.set('sourceCode', filter.sourceCode);
  if (filter.queue) query.set('queue', filter.queue);
  if (filter.ruleCode) query.set('ruleCode', filter.ruleCode);
  if (filter.overdueOnly) query.set('overdueOnly', 'true');

  return apiGet<PagedResult<CaseListItemDto>>(`/api/v1/remediation/cases?${query.toString()}`);
};

export const getCase = (caseId: string) => apiGet<CaseDetailDto>(`/api/v1/remediation/cases/${caseId}`);

export const getFunnel = () => apiGet<RemediationFunnelDto>('/api/v1/remediation/funnel');

export const generateCases = (runId?: string) =>
  apiPost<CaseGenerationDto>('/api/v1/remediation/cases/generate', { runId: runId || null });

export const proposeCorrection = (
  caseId: string,
  proposal: {
    country?: string;
    townName?: string;
    postCode?: string;
    streetName?: string;
    buildingNumber?: string;
    notes?: string;
  },
) => apiPut<CaseDetailDto>(`/api/v1/remediation/cases/${caseId}/proposal`, proposal);

export const addEvidence = (caseId: string, evidence: { kind: string; reference: string; description?: string }) =>
  apiPost<CaseDetailDto>(`/api/v1/remediation/cases/${caseId}/evidence`, evidence);

export const submitCase = (caseId: string) =>
  apiPost<CaseDetailDto>(`/api/v1/remediation/cases/${caseId}/submit`);

export const decideCase = (
  caseId: string,
  decision: DecisionType,
  rationale: string,
  exceptionExpiresOn?: string,
) =>
  apiPost<CaseDetailDto>(`/api/v1/remediation/cases/${caseId}/decision`, {
    decision,
    rationale: rationale || null,
    exceptionExpiresOn: exceptionExpiresOn || null,
  });

export const previewBulk = (action: string, selection: BulkSelection) =>
  apiPost<BulkPreviewDto>('/api/v1/remediation/bulk/preview', { action, selection });

export const applyBulk = (action: string, selection: BulkSelection, rationale?: string) =>
  apiPost<BulkResultDto>('/api/v1/remediation/bulk/apply', { action, selection, rationale: rationale || null });

export const getWriteBackTargets = () =>
  apiGet<WriteBackTargetDto[]>('/api/v1/remediation/writeback/targets');

export const previewWriteBack = (sourceCode: string) =>
  apiPost<WriteBackPreviewDto>('/api/v1/remediation/writeback/preview', { sourceCode, caseIds: null });

export const applyWriteBack = (sourceCode: string, idempotencyKey: string) =>
  apiPost<WriteBackJobDto>('/api/v1/remediation/writeback/apply', {
    sourceCode,
    caseIds: null,
    idempotencyKey,
  });

export const getWriteBackJobs = (page: number, sourceCode?: string) => {
  const query = new URLSearchParams({ page: String(page) });
  if (sourceCode) query.set('sourceCode', sourceCode);

  return apiGet<PagedResult<WriteBackJobDto>>(`/api/v1/remediation/writeback/jobs?${query.toString()}`);
};

export const reconcileWriteBack = (jobId: string) =>
  apiGet<WriteBackReconciliationDto>(`/api/v1/remediation/writeback/jobs/${jobId}/reconciliation`);

export const rollbackWriteBack = (jobId: string, reason: string) =>
  apiPost<WriteBackJobDto>(`/api/v1/remediation/writeback/jobs/${jobId}/rollback`, { reason });
