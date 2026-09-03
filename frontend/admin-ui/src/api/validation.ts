import { apiGet, apiPost } from './client';
import type { PartyRole } from './ingestion';
import type { PagedResult } from './releases';

export type AddressClassification = 'Structured' | 'Hybrid' | 'Unstructured' | 'Absent' | 'Unrecognized';
export type RuleMode = 'Current' | 'Future';
export type IssueSeverity = 'Info' | 'Warning' | 'Error';
export type RecordOutcome =
  | 'Compliant'
  | 'Informational'
  | 'Warning'
  | 'Rejected'
  | 'Excluded'
  | 'UnableToAssess';
export type ValidationRunStatus = 'Running' | 'Completed' | 'Failed';
export type ProfileDimension = 'Scheme' | 'Source' | 'PartyRole' | 'Country' | 'Classification' | 'Issue';

export const recordOutcomes: RecordOutcome[] = [
  'Compliant',
  'Informational',
  'Warning',
  'Rejected',
  'Excluded',
  'UnableToAssess',
];

export const profileDimensions: ProfileDimension[] = [
  'Scheme',
  'Source',
  'PartyRole',
  'Country',
  'Classification',
  'Issue',
];

export const classifications: AddressClassification[] = [
  'Structured',
  'Hybrid',
  'Unstructured',
  'Absent',
  'Unrecognized',
];

export interface ValidationRunDto {
  id: string;
  batchId: string;
  sourceCode: string;
  schemeCode: string;
  asOf: string;
  currentRulesetVersion?: number | null;
  futureRulesetVersion?: number | null;
  status: ValidationRunStatus;
  errorSummary?: string | null;
  inputRecordCount: number;
  assessedCount: number;
  excludedCount: number;
  unableToAssessCount: number;
  currentCompliantCount: number;
  currentWarningCount: number;
  currentRejectedCount: number;
  futureCompliantCount: number;
  futureWarningCount: number;
  futureRejectedCount: number;
  currentReadinessPercent: number;
  futureReadinessPercent: number;
  paymentsAtRisk: number;
  countsReconcile: boolean;
  startedAtUtc: string;
  completedAtUtc?: string | null;
}

export interface ValidationIssueDto {
  id: string;
  mode: RuleMode;
  ruleCode: string;
  field: string;
  severity: IssueSeverity;
  message: string;
  expected?: string | null;
  actual?: string | null;
}

export interface AddressAssessmentDto {
  id: string;
  runId: string;
  recordId: string;
  batchId: string;
  sourceCode: string;
  sequence: number;
  messageId?: string | null;
  endToEndId?: string | null;
  partyRole: PartyRole;
  partyName?: string | null;
  country?: string | null;
  townName?: string | null;
  postCode?: string | null;
  streetName?: string | null;
  buildingNumber?: string | null;
  addressLines?: string | null;
  schemeCode?: string | null;
  isDuplicate: boolean;
  classification: AddressClassification;
  currentOutcome: RecordOutcome;
  futureOutcome: RecordOutcome;
  evidencePointer: string;
  issues: ValidationIssueDto[];
}

export interface ProfileRowDto {
  key: string;
  recordCount: number;
  currentRejectedCount: number;
  futureRejectedCount: number;
  currentReadinessPercent: number;
  futureReadinessPercent: number;
}

export interface ProfileDto {
  dimension: ProfileDimension;
  rows: ProfileRowDto[];
  asOfUtc: string;
}

export interface IssueSummaryDto {
  ruleCode: string;
  field: string;
  severity: IssueSeverity;
  mode: RuleMode;
  count: number;
}

export interface ReadinessSummaryDto {
  runCount: number;
  assessedCount: number;
  excludedCount: number;
  unableToAssessCount: number;
  currentRejectedCount: number;
  futureRejectedCount: number;
  currentReadinessPercent: number;
  futureReadinessPercent: number;
  paymentsAtRisk: number;
  topIssues: IssueSummaryDto[];
  asOfUtc: string;
}

export interface AssessmentFilter {
  mode: RuleMode;
  outcome?: RecordOutcome | '';
  classification?: AddressClassification | '';
  ruleCode?: string;
}

export const getRuns = (page: number, sourceCode?: string) => {
  const query = new URLSearchParams({ page: String(page) });
  if (sourceCode) query.set('sourceCode', sourceCode);

  return apiGet<PagedResult<ValidationRunDto>>(`/api/v1/validation/runs?${query.toString()}`);
};

export const getAssessments = (runId: string, page: number, filter: AssessmentFilter) => {
  const query = new URLSearchParams({ page: String(page), mode: filter.mode });
  if (filter.outcome) query.set('outcome', filter.outcome);
  if (filter.classification) query.set('classification', filter.classification);
  if (filter.ruleCode) query.set('ruleCode', filter.ruleCode);

  return apiGet<PagedResult<AddressAssessmentDto>>(
    `/api/v1/validation/runs/${runId}/assessments?${query.toString()}`,
  );
};

export const getReadiness = () => apiGet<ReadinessSummaryDto>('/api/v1/validation/readiness');

export const getProfile = (dimension: ProfileDimension, runId?: string) =>
  apiGet<ProfileDto>(`/api/v1/validation/profile?dimension=${dimension}${runId ? `&runId=${runId}` : ''}`);

export const runValidation = (batchId: string, asOf?: string) =>
  apiPost<ValidationRunDto>('/api/v1/validation/runs', { batchId, asOf: asOf || null });
