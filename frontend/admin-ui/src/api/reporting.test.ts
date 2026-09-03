import { describe, expect, it, vi } from 'vitest';

const apiGet = vi.fn();
const apiDownload = vi.fn();
vi.mock('./client', () => ({
  apiGet: (path: string) => apiGet(path),
  apiDownload: (path: string, fileName: string) => apiDownload(path, fileName),
}));

const { drillDown, exportDashboard, getDashboard, scopeQuery } = await import('./reporting');

describe('reporting api', () => {
  it('drops empty filters so the same view always resolves to the same scope key', () => {
    expect(scopeQuery({ schemeCodes: 'SEPA', sourceCodes: '', asOf: '' })).toBe('?schemeCodes=SEPA');
    expect(scopeQuery({})).toBe('');
  });

  it('asks for a fresh snapshot only when a refresh was requested', async () => {
    await getDashboard('Executive', { schemeCodes: 'SEPA' });
    expect(apiGet).toHaveBeenCalledWith('/api/v1/reporting/dashboards/executive?schemeCodes=SEPA');

    await getDashboard('Executive', {}, true);
    expect(apiGet).toHaveBeenCalledWith('/api/v1/reporting/dashboards/executive?refresh=true');
  });

  it('keeps the drill-down scope identical to the dashboard scope', async () => {
    await drillDown('Scheme', 'Source', { countries: 'DE', exclusions: 'DORMANT' });

    expect(apiGet).toHaveBeenCalledWith(
      '/api/v1/reporting/dashboards/scheme/drill/source?countries=DE&exclusions=DORMANT',
    );
  });

  it('exports through the token-aware download so the CSV is not fetched anonymously', async () => {
    await exportDashboard('Operations', { asOf: '2026-01-31' });

    expect(apiDownload).toHaveBeenCalledWith(
      '/api/v1/reporting/dashboards/operations/export?asOf=2026-01-31',
      'operations-dashboard.csv',
    );
  });
});
