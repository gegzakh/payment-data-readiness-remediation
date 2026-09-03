import { apiGet, apiPost } from './client';

export type RuleKind = 'Required' | 'MaxLength' | 'Pattern' | 'AllowedValues' | 'Prohibited' | 'StructuredOnly';
export type RuleSeverity = 'Info' | 'Warning' | 'Error';
export type RuleApplicability = 'Current' | 'Future' | 'Both';
export type RulesetStatus = 'Draft' | 'Active' | 'Retired';

export const ruleKinds: RuleKind[] = [
  'Required',
  'MaxLength',
  'Pattern',
  'AllowedValues',
  'Prohibited',
  'StructuredOnly',
];

export const ruleSeverities: RuleSeverity[] = ['Info', 'Warning', 'Error'];
export const ruleApplicabilities: RuleApplicability[] = ['Current', 'Future', 'Both'];

export interface SchemeDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  structuredAddressMandatoryFrom?: string | null;
  isActive: boolean;
}

export interface RuleDto {
  id: string;
  code: string;
  field: string;
  kind: RuleKind;
  severity: RuleSeverity;
  applicability: RuleApplicability;
  message: string;
  parameter?: string | null;
}

export interface RulesetVersionDto {
  id: string;
  versionNumber: number;
  status: RulesetStatus;
  notes?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  activatedAtUtc?: string | null;
  activatedBy?: string | null;
  rules: RuleDto[];
}

export interface RulesetDto {
  id: string;
  schemeCode: string;
  name: string;
  description?: string | null;
  activeVersionNumber?: number | null;
  versions: RulesetVersionDto[];
}

export interface RuleInput {
  code: string;
  field: string;
  kind: RuleKind;
  severity: RuleSeverity;
  applicability: RuleApplicability;
  message: string;
  parameter?: string | null;
}

export const getSchemes = () => apiGet<SchemeDto[]>('/api/v1/schemes');

export const getRulesets = () => apiGet<RulesetDto[]>('/api/v1/rulesets');

export const addVersion = (rulesetId: string, copyFromVersionNumber: number | null, notes: string | null) =>
  apiPost<number>(`/api/v1/rulesets/${rulesetId}/versions`, { copyFromVersionNumber, notes });

export const addRule = (rulesetId: string, versionNumber: number, rule: RuleInput) =>
  apiPost<string>(`/api/v1/rulesets/${rulesetId}/versions/${versionNumber}/rules`, rule);

/** Activating a retired version is the rollback path, so the same call covers both. */
export const activateVersion = (rulesetId: string, versionNumber: number, effectiveFrom: string) =>
  apiPost<void>(`/api/v1/rulesets/${rulesetId}/versions/${versionNumber}/activate`, { effectiveFrom });
