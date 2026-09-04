import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  applyWriteBack,
  getWriteBackJobs,
  getWriteBackTargets,
  previewWriteBack,
  reconcileWriteBack,
  rollbackWriteBack,
  type WriteBackPreviewDto,
  type WriteBackReconciliationDto,
} from '../api/remediation';
import { hasPermission } from '../auth/keycloak';

/** A run key the source can deduplicate on, so a retry never writes the same correction twice. */
const newIdempotencyKey = () => `ui-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;

export function WriteBackPage() {
  const queryClient = useQueryClient();
  const canWriteBack = hasPermission('remediation.writeback');

  const [sourceCode, setSourceCode] = useState('');
  const [page, setPage] = useState(1);
  const [expanded, setExpanded] = useState<string | null>(null);
  const [reason, setReason] = useState('');
  const [preview, setPreview] = useState<WriteBackPreviewDto | null>(null);
  const [reconciliation, setReconciliation] = useState<WriteBackReconciliationDto | null>(null);

  const targets = useQuery({ queryKey: ['writeback-targets'], queryFn: getWriteBackTargets });
  const jobs = useQuery({ queryKey: ['writeback-jobs', page], queryFn: () => getWriteBackJobs(page) });

  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ['writeback-jobs'] });

  const runPreview = useMutation({
    mutationFn: () => previewWriteBack(sourceCode),
    onSuccess: (result) => setPreview(result ?? null),
  });
  const apply = useMutation({
    mutationFn: () => applyWriteBack(sourceCode, newIdempotencyKey()),
    onSuccess: () => {
      setPreview(null);
      invalidate();
    },
  });
  const reconcile = useMutation({
    mutationFn: (jobId: string) => reconcileWriteBack(jobId),
    onSuccess: (result) => setReconciliation(result ?? null),
  });
  const rollback = useMutation({
    mutationFn: (jobId: string) => rollbackWriteBack(jobId, reason),
    onSuccess: invalidate,
  });

  return (
    <section>
      <h1>Write-back</h1>
      <p className="muted">
        Approved corrections are pushed to the owning source, read back to prove they landed, and can be
        reversed. Only the fields a source authorises are ever written.
      </p>

      {targets.isError && <p className="error">{targets.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Source</th>
            <th>Mode</th>
            <th>Writable fields</th>
            <th>Maintenance window</th>
            <th>Records per run</th>
            <th>Approval required</th>
            <th>Rollback</th>
            <th>Enabled</th>
          </tr>
        </thead>
        <tbody>
          {targets.data?.map((target) => (
            <tr key={target.id}>
              <td>
                <button className="link" onClick={() => setSourceCode(target.sourceCode)} type="button">
                  {target.sourceCode}
                </button>
              </td>
              <td>{target.mode}</td>
              <td>{target.writableFields}</td>
              <td>{target.maintenanceWindow ?? '—'}</td>
              <td>{target.maxRecordsPerRun}</td>
              <td>{target.requiresApproval ? 'yes' : 'no'}</td>
              <td>{target.rollbackMethod}</td>
              <td className={target.isEnabled ? undefined : 'error'}>{target.isEnabled ? 'yes' : 'no'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="card">
        <h2>Run a write-back</h2>
        <div className="filters">
          <label>
            Source <input onChange={(event) => setSourceCode(event.target.value.toUpperCase())} value={sourceCode} />
          </label>
          <button disabled={!sourceCode || runPreview.isPending} onClick={() => runPreview.mutate()} type="button">
            Preview
          </button>
          {canWriteBack && (
            <button
              disabled={!preview || preview.recordsToWrite === 0 || apply.isPending}
              onClick={() => apply.mutate()}
              type="button"
            >
              Apply {preview?.recordsToWrite ?? 0} records
            </button>
          )}
        </div>
        {runPreview.isError && <p className="error">{runPreview.error.message}</p>}
        {apply.isError && <p className="error">{apply.error.message}</p>}
        {preview && (
          <>
            <p>
              {preview.eligibleCases} approved cases, {preview.recordsToWrite} records, {preview.mode} mode,
              rollback by {preview.rollbackMethod}
              {preview.maintenanceWindow ? `, avoid ${preview.maintenanceWindow}` : ''}.
            </p>
            {preview.blockers.length > 0 && (
              <ul>
                {preview.blockers.map((blocker) => (
                  <li className="error" key={blocker}>
                    {blocker}
                  </li>
                ))}
              </ul>
            )}
            <table className="table">
              <thead>
                <tr>
                  <th>Record</th>
                  <th>Field</th>
                  <th>Before</th>
                  <th>After</th>
                </tr>
              </thead>
              <tbody>
                {preview.changes.map((change) => (
                  <tr key={`${change.recordReference}-${change.field}`}>
                    <td>{change.recordReference}</td>
                    <td>{change.field}</td>
                    <td>{change.beforeValue ?? '—'}</td>
                    <td>{change.afterValue ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}
      </div>

      <h2>Runs</h2>
      {jobs.isError && <p className="error">{jobs.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Requested</th>
            <th>Source</th>
            <th>Status</th>
            <th>Records</th>
            <th>Confirmed</th>
            <th>Failed</th>
            <th>Stale</th>
            <th>Rolled back</th>
            <th>Reconciles</th>
            <th>Checksum</th>
          </tr>
        </thead>
        <tbody>
          {jobs.data?.items.map((job) => (
            <tr key={job.id}>
              <td>
                <button
                  className="link"
                  onClick={() => {
                    setReconciliation(null);
                    setExpanded(expanded === job.id ? null : job.id);
                  }}
                  type="button"
                >
                  {job.requestedAtUtc.replace('T', ' ').slice(0, 19)}
                </button>
              </td>
              <td>{job.targetSourceCode}</td>
              <td className={job.status === 'Failed' || job.status === 'PartiallyFailed' ? 'error' : undefined}>
                {job.status}
              </td>
              <td>{job.itemCount}</td>
              <td>{job.confirmedCount}</td>
              <td className={job.failedCount > 0 ? 'error' : undefined}>{job.failedCount}</td>
              <td>{job.staleCount}</td>
              <td>{job.rolledBackCount}</td>
              <td className={job.countsReconcile ? undefined : 'error'}>{job.countsReconcile ? 'yes' : 'no'}</td>
              <td className="muted">{job.exportChecksum ? job.exportChecksum.slice(0, 12) : '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {jobs.data && (
        <p className="pagination">
          <button disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">
            Previous
          </button>
          Page {jobs.data.page} of {Math.max(jobs.data.totalPages, 1)}
          <button disabled={page >= jobs.data.totalPages} onClick={() => setPage(page + 1)} type="button">
            Next
          </button>
        </p>
      )}

      {expanded &&
        jobs.data?.items
          .filter((job) => job.id === expanded)
          .map((job) => (
            <div className="card" key={job.id}>
              <h2>Run {job.idempotencyKey}</h2>
              <p className="muted">
                requested by {job.requestedBy}
                {job.failureSummary ? ` · ${job.failureSummary}` : ''}
              </p>
              <div className="filters">
                <button onClick={() => reconcile.mutate(job.id)} type="button">
                  Reconcile against the source
                </button>
                {canWriteBack && (
                  <>
                    <label>
                      Rollback reason <input onChange={(event) => setReason(event.target.value)} value={reason} />
                    </label>
                    <button disabled={!reason || rollback.isPending} onClick={() => rollback.mutate(job.id)} type="button">
                      Roll back
                    </button>
                  </>
                )}
              </div>
              {rollback.isError && <p className="error">{rollback.error.message}</p>}
              {reconcile.isError && <p className="error">{reconcile.error.message}</p>}
              {reconciliation && reconciliation.jobId === job.id && (
                <p className={reconciliation.balanced ? undefined : 'error'}>
                  requested {reconciliation.requested}, applied {reconciliation.applied}, confirmed{' '}
                  {reconciliation.confirmed}, failed {reconciliation.failed}, stale {reconciliation.stale},
                  rolled back {reconciliation.rolledBack}
                  {reconciliation.discrepancies.length > 0 && ` — ${reconciliation.discrepancies.join('; ')}`}
                </p>
              )}
              <table className="table">
                <thead>
                  <tr>
                    <th>Record</th>
                    <th>Status</th>
                    <th>Before</th>
                    <th>After</th>
                    <th>Version</th>
                    <th>Correlation</th>
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  {job.items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.recordReference}</td>
                      <td className={item.status === 'Failed' || item.status === 'Stale' ? 'error' : undefined}>
                        {item.status}
                      </td>
                      <td>{item.beforeValue}</td>
                      <td>{item.afterValue}</td>
                      <td>{item.sourceVersion ?? '—'}</td>
                      <td className="muted">{item.correlationId ?? '—'}</td>
                      <td>{item.message ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
    </section>
  );
}
