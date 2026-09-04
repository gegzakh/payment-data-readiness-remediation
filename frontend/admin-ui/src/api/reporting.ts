import { apiDownload, apiGet } from './client';

export type DashboardAudience =
  | 'Executive'
  | 'Scheme'
  | 'Source'
  | 'Operations'
  | 'Remediation'
  | 'Testing'
  | 'Cutover';

export type MetricUnit = 'Count' | 'Percent' | 'Money' | 'Text';
export type MetricDirection = 'HigherIsBetter' | 'LowerIsBetter' | 'Neutral';
export type ReconciliationStatus = 'Reconciled' | 'Partial' | 'Unreconciled';

export const dashboardAudiences: DashboardAudience[] = [
  'Executive',
  'Scheme',
  'Source',
  'Operations',
  'Remediation',
  'Testing',
  'Cutover',
];

export interface MetricDto {
  key: string;
  label: string;
  value: number;
  unit: MetricUnit;
  direction: MetricDirection;
  drillDimension?: string | null;
  text?: string | null;
}

export interface BreakdownRowDto {
  dimension: string;
  key: string;
  recordCount: number;
  rejectedCount: number;
  warningCount: number;
  paymentsAtRisk: number;
  readinessPercent: number;
}

export interface DashboardDto {
  id: string;
  audience: DashboardAudience;
  scopeKey: string;
  scopeDescription: string;
  schemeCodes?: string | null;
  sourceCodes?: string | null;
  countries?: string | null;
  exclusions?: string | null;
  asOf?: string | null;
  capturedAtUtc: string;
  sourceAsOfUtc?: string | null;
  rulesetVersion?: string | null;
  reconciliation: ReconciliationStatus;
  reconciliationNote?: string | null;
  metrics: MetricDto[];
  breakdown: BreakdownRowDto[];
}

export interface DrillDownDto {
  audience: DashboardAudience;
  dimension: string;
  scopeDescription: string;
  capturedAtUtc: string;
  sourceAsOfUtc?: string | null;
  rulesetVersion?: string | null;
  reconciliation: ReconciliationStatus;
  rows: BreakdownRowDto[];
}

export interface DashboardScope {
  schemeCodes?: string;
  sourceCodes?: string;
  countries?: string;
  exclusions?: string;
  asOf?: string;
}

/** Only the filters the user actually set travel with the request, so the scope key stays stable. */
export function scopeQuery(scope: DashboardScope, extra: Record<string, string> = {}): string {
  const params = new URLSearchParams();
  Object.entries({ ...scope, ...extra }).forEach(([key, value]) => {
    if (value) {
      params.set(key, value);
    }
  });
  const query = params.toString();
  return query ? `?${query}` : '';
}

export const getDashboard = (audience: DashboardAudience, scope: DashboardScope, refresh = false) =>
  apiGet<DashboardDto>(
    `/api/v1/reporting/dashboards/${audience.toLowerCase()}${scopeQuery(scope, refresh ? { refresh: 'true' } : {})}`,
  );

export const drillDown = (audience: DashboardAudience, dimension: string, scope: DashboardScope) =>
  apiGet<DrillDownDto>(
    `/api/v1/reporting/dashboards/${audience.toLowerCase()}/drill/${dimension.toLowerCase()}${scopeQuery(scope)}`,
  );

export const exportDashboard = (audience: DashboardAudience, scope: DashboardScope) =>
  apiDownload(
    `/api/v1/reporting/dashboards/${audience.toLowerCase()}/export${scopeQuery(scope)}`,
    `${audience.toLowerCase()}-dashboard.csv`,
  );
