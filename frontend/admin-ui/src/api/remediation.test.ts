import { describe, expect, it, vi } from 'vitest';

const apiGet = vi.fn();
const apiPost = vi.fn();
const apiPut = vi.fn();
vi.mock('./client', () => ({
  apiGet: (path: string) => apiGet(path),
  apiPost: (path: string, body?: unknown) => apiPost(path, body),
  apiPut: (path: string, body?: unknown) => apiPut(path, body),
}));

const { decideCase, generateCases, getCases, previewBulk } = await import('./remediation');

describe('remediation api', () => {
  it('sends only the queue filters that are set', async () => {
    await getCases(3, { status: 'PendingApproval', priority: '', sourceCode: 'CBS', overdueOnly: true });

    expect(apiGet).toHaveBeenCalledWith(
      '/api/v1/remediation/cases?page=3&status=PendingApproval&sourceCode=CBS&overdueOnly=true',
    );
  });

  it('treats an empty run id as "generate from the latest runs"', async () => {
    await generateCases('');

    expect(apiPost).toHaveBeenCalledWith('/api/v1/remediation/cases/generate', { runId: null });
  });

  it('omits an unset exception expiry so the API applies its own rule', async () => {
    await decideCase('case-1', 'Approve', 'Matches the register');

    expect(apiPost).toHaveBeenCalledWith('/api/v1/remediation/cases/case-1/decision', {
      decision: 'Approve',
      rationale: 'Matches the register',
      exceptionExpiresOn: null,
    });
  });

  it('passes the bulk selection through untouched so the preview matches the apply', async () => {
    const selection = { sourceCode: 'CBS', status: 'PendingApproval' as const, minimumConfidence: 90 };
    await previewBulk('approve', selection);

    expect(apiPost).toHaveBeenCalledWith('/api/v1/remediation/bulk/preview', { action: 'approve', selection });
  });
});
