import { apiGet } from './client';
import type { PagedResult } from './releases';

export type AuditOutcome = 'Success' | 'Failure' | 'Denied';

export const auditOutcomes: AuditOutcome[] = ['Success', 'Failure', 'Denied'];

export interface AuditRecordDto {
  id: string;
  sequence: number;
  occurredAtUtc: string;
  service: string;
  action: string;
  entityType: string;
  entityId: string;
  actor: string;
  actorId?: string | null;
  outcome: AuditOutcome;
  correlationId?: string | null;
  legalEntity?: string | null;
  details?: string | null;
  previousHash: string;
  hash: string;
}

export interface AuditChainVerificationDto {
  isIntact: boolean;
  recordsChecked: number;
  firstBrokenSequence?: number | null;
  verifiedAtUtc: string;
}

export interface AuditFilter {
  service?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  actor?: string;
  correlationId?: string;
  outcome?: AuditOutcome | '';
}

export const getAuditRecords = (page: number, filter: AuditFilter) => {
  const query = new URLSearchParams({ page: String(page) });
  for (const [key, value] of Object.entries(filter)) {
    if (value) {
      query.set(key, value);
    }
  }

  return apiGet<PagedResult<AuditRecordDto>>(`/api/v1/audit?${query.toString()}`);
};

export const verifyAuditChain = () => apiGet<AuditChainVerificationDto>('/api/v1/audit/verify');
