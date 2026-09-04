import { apiGet, apiPost } from './client';

export type SourceKind = 'PaymentHub' | 'Erp' | 'Crm' | 'MasterData' | 'FileFeed' | 'Channel';
export type InterfaceKind = 'Api' | 'Sftp' | 'Database' | 'Upload' | 'Event';
export type OnboardingStatus = 'Registered' | 'Onboarding' | 'Scanning' | 'Ready' | 'Blocked';
export type MappingReadiness = 'NotStarted' | 'InProgress' | 'Ready' | 'NeedsRework';

export const onboardingStatuses: OnboardingStatus[] = [
  'Registered',
  'Onboarding',
  'Scanning',
  'Ready',
  'Blocked',
];

export interface FieldMappingDto {
  id: string;
  sourceAttribute: string;
  targetElement: string;
  transformation?: string | null;
  isAuthoritative: boolean;
  notes?: string | null;
  lastReviewedAtUtc?: string | null;
}

export interface LineageStepDto {
  sequence: number;
  fromNode: string;
  toNode: string;
  channel?: string | null;
  description?: string | null;
}

export interface SourceSystemDto {
  id: string;
  code: string;
  name: string;
  kind: SourceKind;
  interface: InterfaceKind;
  ownerName: string;
  ownerEmail: string;
  legalEntity: string;
  schemeCodes: string[];
  schedule?: string | null;
  estimatedPartyCount: number;
  recurringInstructionCount: number;
  isAuthoritative: boolean;
  status: OnboardingStatus;
  mapping: MappingReadiness;
  scanCoveragePercent: number;
  lastScanAtUtc?: string | null;
  lastAttestedAtUtc?: string | null;
  lastAttestedBy?: string | null;
  attestationOverdue: boolean;
  readinessScore: number;
  remediationOwner?: string | null;
  isActive: boolean;
  mappings: FieldMappingDto[];
  lineage: LineageStepDto[];
}

export interface SourceReadinessSummaryDto {
  totalSources: number;
  readySources: number;
  blockedSources: number;
  attestationOverdueSources: number;
  coveredPartyCount: number;
  totalPartyCount: number;
  averageReadinessScore: number;
  asOfUtc: string;
}

export interface SourceFilter {
  schemeCode?: string;
  status?: OnboardingStatus | '';
  attestationOverdueOnly?: boolean;
}

export function getSources(filter: SourceFilter = {}) {
  const query = new URLSearchParams();
  if (filter.schemeCode) query.set('schemeCode', filter.schemeCode);
  if (filter.status) query.set('status', filter.status);
  if (filter.attestationOverdueOnly) query.set('attestationOverdueOnly', 'true');

  return apiGet<SourceSystemDto[]>(`/api/v1/sources?${query.toString()}`);
}

export const getSourceReadiness = () => apiGet<SourceReadinessSummaryDto>('/api/v1/sources/readiness');

export const attestSource = (code: string) => apiPost<void>(`/api/v1/sources/${code}/attestation`);

export const recordScan = (code: string, coveragePercent: number) =>
  apiPost<void>(`/api/v1/sources/${code}/scan`, { coveragePercent });
