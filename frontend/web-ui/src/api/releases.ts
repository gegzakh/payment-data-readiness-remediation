import { apiGet } from './client';

export type ReleaseEntryType = 'Feature' | 'Change' | 'Fix' | 'Security' | 'Deprecation' | 'Erratum';

export interface ReleaseEntryDto {
  id: string;
  type: ReleaseEntryType;
  component: string;
  title: string;
  body?: string | null;
  references: string[];
}

export interface ReleaseEntryGroupDto {
  component: string;
  entries: ReleaseEntryDto[];
}

export interface ReleaseDto {
  id: string;
  version: string;
  title: string;
  releaseDate: string;
  status: 'Draft' | 'Published';
  summary?: string | null;
  publishedAtUtc?: string | null;
  publishedBy?: string | null;
  groups: ReleaseEntryGroupDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ReleaseQuery {
  page: number;
  pageSize?: number;
  type?: ReleaseEntryType;
  component?: string;
  from?: string;
  to?: string;
}

export function releasesPath(query: ReleaseQuery): string {
  const params = new URLSearchParams({ page: String(query.page) });
  if (query.pageSize) params.set('pageSize', String(query.pageSize));
  if (query.type) params.set('type', query.type);
  if (query.component) params.set('component', query.component);
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  return `/api/v1/releases?${params.toString()}`;
}

export const getReleases = (query: ReleaseQuery) => apiGet<PagedResult<ReleaseDto>>(releasesPath(query));

export const getAllowedPageSizes = () => apiGet<number[]>('/api/v1/releases/page-sizes');
