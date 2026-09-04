import { describe, expect, it, vi } from 'vitest';

const apiGet = vi.fn();
vi.mock('./client', () => ({ apiGet: (path: string) => apiGet(path) }));

const { getAuditRecords } = await import('./audit');

describe('getAuditRecords', () => {
  it('sends only the filters that are set', async () => {
    await getAuditRecords(3, { service: 'rules', actor: '', outcome: 'Denied' });

    expect(apiGet).toHaveBeenCalledWith('/api/v1/audit?page=3&service=rules&outcome=Denied');
  });
});
