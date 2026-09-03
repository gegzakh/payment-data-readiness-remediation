import { describe, expect, it, vi } from 'vitest';

const apiGet = vi.fn();
const apiPost = vi.fn();
vi.mock('./client', () => ({
  apiGet: (path: string) => apiGet(path),
  apiPost: (path: string, body?: unknown) => apiPost(path, body),
}));

const { getAssessments, getProfile, runValidation } = await import('./validation');

describe('validation api', () => {
  it('sends only the assessment filters that are set', async () => {
    await getAssessments('run-1', 2, { mode: 'Current', outcome: 'Rejected', classification: '', ruleCode: '' });

    expect(apiGet).toHaveBeenCalledWith(
      '/api/v1/validation/runs/run-1/assessments?page=2&mode=Current&outcome=Rejected',
    );
  });

  it('omits the run id when profiling the whole portfolio', async () => {
    await getProfile('Country');

    expect(apiGet).toHaveBeenCalledWith('/api/v1/validation/profile?dimension=Country');
  });

  it('treats an empty as-of date as "use the configured cutover date"', async () => {
    await runValidation('batch-1', '');

    expect(apiPost).toHaveBeenCalledWith('/api/v1/validation/runs', { batchId: 'batch-1', asOf: null });
  });
});
