import { keycloak } from '../auth/keycloak';
import { ApiError, apiGet, apiPost } from './client';
import type { PagedResult } from './releases';

export type IngestionFormat = 'Iso20022Xml' | 'Csv';
export type IngestionChannel = 'Upload' | 'Api' | 'Sftp' | 'ObjectStorage' | 'Event' | 'Database';
export type BatchStatus = 'Received' | 'Quarantined' | 'Parsing' | 'Parsed' | 'Failed' | 'Cancelled';
export type PartyRole = 'Debtor' | 'Creditor' | 'UltimateDebtor' | 'UltimateCreditor';

export const ingestionFormats: IngestionFormat[] = ['Iso20022Xml', 'Csv'];
export const batchStatuses: BatchStatus[] = [
  'Received',
  'Quarantined',
  'Parsing',
  'Parsed',
  'Failed',
  'Cancelled',
];

export interface IngestionBatchDto {
  id: string;
  sourceCode: string;
  fileName: string;
  format: IngestionFormat;
  channel: IngestionChannel;
  sizeBytes: number;
  checksum: string;
  idempotencyKey: string;
  parserVersion: string;
  submittedBy: string;
  isReprocess: boolean;
  status: BatchStatus;
  quarantineReason?: string | null;
  errorSummary?: string | null;
  recordCount: number;
  parsedCount: number;
  failedCount: number;
  duplicateCount: number;
  excludedCount: number;
  checkpoint: number;
  retryCount: number;
  countsReconcile: boolean;
  receivedAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
}

export interface PartyAddressRecordDto {
  id: string;
  batchId: string;
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
  contentHash: string;
  isDuplicate: boolean;
}

export interface IngestionOverviewDto {
  totalBatches: number;
  parsedBatches: number;
  quarantinedBatches: number;
  failedBatches: number;
  totalRecords: number;
  duplicateRecords: number;
  asOfUtc: string;
}

export const getBatches = (page: number, status?: BatchStatus | '', sourceCode?: string) => {
  const query = new URLSearchParams({ page: String(page) });
  if (status) query.set('status', status);
  if (sourceCode) query.set('sourceCode', sourceCode);

  return apiGet<PagedResult<IngestionBatchDto>>(`/api/v1/batches?${query.toString()}`);
};

export const getOverview = () => apiGet<IngestionOverviewDto>('/api/v1/batches/overview');

export const getBatchRecords = (batchId: string, page: number, duplicatesOnly = false) =>
  apiGet<PagedResult<PartyAddressRecordDto>>(
    `/api/v1/batches/${batchId}/records?page=${page}&duplicatesOnly=${duplicatesOnly}`,
  );

export const retryBatch = (batchId: string) => apiPost<IngestionBatchDto>(`/api/v1/batches/${batchId}/retry`);

export const cancelBatch = (batchId: string) => apiPost<IngestionBatchDto>(`/api/v1/batches/${batchId}/cancel`);

/** Uploads go through fetch directly: the payload is multipart, not JSON. */
export async function uploadBatch(
  file: File,
  sourceCode: string,
  format: IngestionFormat,
  reprocess: boolean,
): Promise<IngestionBatchDto> {
  await keycloak.updateToken(30).catch(() => keycloak.login());

  const body = new FormData();
  body.append('file', file);

  const query = new URLSearchParams({ sourceCode, format, reprocess: String(reprocess) });
  const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? ''}/api/v1/batches/upload?${query}`, {
    method: 'POST',
    headers: { Accept: 'application/json', Authorization: `Bearer ${keycloak.token}` },
    body,
  });

  if (!response.ok) {
    throw new ApiError(response.status, await response.text());
  }

  return (await response.json()) as IngestionBatchDto;
}
