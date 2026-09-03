import { apiGet, apiPost, apiPut } from './client';

export type ReleaseEntryType = 'Feature' | 'Change' | 'Fix' | 'Security' | 'Deprecation' | 'Erratum';

export const releaseEntryTypes: ReleaseEntryType[] = [
  'Feature',
  'Change',
  'Fix',
  'Security',
  'Deprecation',
  'Erratum',
];

export interface ReleaseEntryDto {
  id: string;
  type: ReleaseEntryType;
  component: string;
  title: string;
  body?: string | null;
  references: string[];
}

export interface ReleaseDto {
  id: string;
  version: string;
  title: string;
  releaseDate: string;
  status: 'Draft' | 'Published';
  summary?: string | null;
  groups: { component: string; entries: ReleaseEntryDto[] }[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ReleaseEntryInput {
  type: ReleaseEntryType;
  component: string;
  title: string;
  body?: string | null;
  sortOrder?: number | null;
  references?: string[];
}

export interface CreateReleaseInput {
  version: string;
  title: string;
  releaseDate: string;
  summary?: string | null;
  entries?: ReleaseEntryInput[];
}

export const getReleases = (page: number, pageSize?: number) =>
  apiGet<PagedResult<ReleaseDto>>(
    `/api/v1/releases?includeDrafts=true&page=${page}${pageSize ? `&pageSize=${pageSize}` : ''}`,
  );

export const createRelease = (input: CreateReleaseInput) => apiPost<string>('/api/v1/admin/releases', input);

export const addEntry = (releaseId: string, entry: ReleaseEntryInput) =>
  apiPost<void>(`/api/v1/admin/releases/${releaseId}/entries`, entry);

export const publishRelease = (releaseId: string) => apiPost<void>(`/api/v1/admin/releases/${releaseId}/publish`);

export const updateSetting = (key: string, value: string) =>
  apiPut<void>(`/api/v1/settings/${encodeURIComponent(key)}`, { value });

export const getSettings = () =>
  apiGet<{ key: string; value: string; valueType: string; description?: string | null }[]>('/api/v1/settings');
