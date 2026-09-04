import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  batchStatuses,
  cancelBatch,
  getBatchRecords,
  getBatches,
  getOverview,
  ingestionFormats,
  retryBatch,
  uploadBatch,
  type BatchStatus,
  type IngestionFormat,
} from '../api/ingestion';
import { runValidation } from '../api/validation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

export function IngestionPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('ingestion.write');
  const canManage = hasPermission('ingestion.manage');
  const canRunValidation = hasPermission('validation.run');

  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<BatchStatus | ''>('');
  const [sourceFilter, setSourceFilter] = useState('');
  const [selected, setSelected] = useState<string | null>(null);
  const [recordPage, setRecordPage] = useState(1);
  const [duplicatesOnly, setDuplicatesOnly] = useState(false);

  const [file, setFile] = useState<File | null>(null);
  const [sourceCode, setSourceCode] = useState('');
  const [format, setFormat] = useState<IngestionFormat>('Iso20022Xml');
  const [reprocess, setReprocess] = useState(false);

  const batches = useQuery({
    queryKey: ['batches', page, status, sourceFilter],
    queryFn: () => getBatches(page, status, sourceFilter || undefined),
  });
  const overview = useQuery({ queryKey: ['ingestion-overview'], queryFn: getOverview });
  const records = useQuery({
    queryKey: ['batch-records', selected, recordPage, duplicatesOnly],
    queryFn: () => getBatchRecords(selected!, recordPage, duplicatesOnly),
    enabled: selected !== null,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['batches'] });
    void queryClient.invalidateQueries({ queryKey: ['ingestion-overview'] });
  };

  const upload = useMutation({
    mutationFn: () => uploadBatch(file!, sourceCode, format, reprocess),
    onSuccess: invalidate,
  });
  const retry = useMutation({ mutationFn: retryBatch, onSuccess: invalidate });
  const cancel = useMutation({ mutationFn: cancelBatch, onSuccess: invalidate });
  const validate = useMutation({
    mutationFn: (batchId: string) => runValidation(batchId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['validation-runs'] }),
  });

  return (
    <section>
      <h1>Ingestion</h1>
      <p className="muted">
        Upload ISO 20022 XML or CSV extracts. Unsafe or oversized payloads are quarantined rather than parsed;
        duplicates are detected by content hash.
      </p>

      {overview.data && (
        <div className="metrics">
          <Metric label="Batches" value={overview.data.totalBatches} />
          <Metric label="Parsed" value={overview.data.parsedBatches} />
          <Metric label="Quarantined" value={overview.data.quarantinedBatches} tone="risk" />
          <Metric label="Failed" value={overview.data.failedBatches} tone="risk" />
          <Metric label="Records" value={overview.data.totalRecords.toLocaleString()} />
          <Metric label="Duplicates" value={overview.data.duplicateRecords.toLocaleString()} />
        </div>
      )}

      {canWrite && (
        <form
          className="card"
          onSubmit={(event) => {
            event.preventDefault();
            if (file && sourceCode) upload.mutate();
          }}
        >
          <h2>Upload a batch</h2>
          <div className="filters">
            <label>
              Source code <input onChange={(event) => setSourceCode(event.target.value)} required value={sourceCode} />
            </label>
            <label>
              Format{' '}
              <select onChange={(event) => setFormat(event.target.value as IngestionFormat)} value={format}>
                {ingestionFormats.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              File{' '}
              <input onChange={(event) => setFile(event.target.files?.[0] ?? null)} required type="file" />
            </label>
            <label>
              Reprocess{' '}
              <input checked={reprocess} onChange={(event) => setReprocess(event.target.checked)} type="checkbox" />
            </label>
          </div>
          <button disabled={upload.isPending || !file || !sourceCode} type="submit">
            {upload.isPending ? 'Uploading…' : 'Upload'}
          </button>
          {upload.isError && <p className="error">{upload.error.message}</p>}
          {upload.data && (
            <p className="muted">
              Batch {upload.data.id.slice(0, 8)} — {upload.data.status}
              {upload.data.quarantineReason ? `: ${upload.data.quarantineReason}` : ''}
            </p>
          )}
        </form>
      )}

      <div className="filters">
        <label>
          Status{' '}
          <select
            onChange={(event) => {
              setPage(1);
              setStatus(event.target.value as BatchStatus | '');
            }}
            value={status}
          >
            <option value="">Any</option>
            {batchStatuses.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        <label>
          Source{' '}
          <input
            onChange={(event) => {
              setPage(1);
              setSourceFilter(event.target.value);
            }}
            value={sourceFilter}
          />
        </label>
      </div>

      {batches.isPending && <p>Loading…</p>}
      {batches.isError && <p className="error">Batches could not be loaded: {batches.error.message}</p>}

      <table className="table">
        <thead>
          <tr>
            <th>Received</th>
            <th>Source</th>
            <th>File</th>
            <th>Format</th>
            <th>Status</th>
            <th>Records</th>
            <th>Parsed</th>
            <th>Dupes</th>
            <th>Failed</th>
            <th>Reconciles</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {batches.data?.items.map((batch) => (
            <tr key={batch.id}>
              <td>{batch.receivedAtUtc.replace('T', ' ').slice(0, 19)}</td>
              <td>{batch.sourceCode}</td>
              <td>
                <button
                  className="link"
                  onClick={() => {
                    setRecordPage(1);
                    setSelected(selected === batch.id ? null : batch.id);
                  }}
                  type="button"
                >
                  {batch.fileName}
                </button>
                {batch.quarantineReason && <div className="error">{batch.quarantineReason}</div>}
                {batch.errorSummary && <div className="error">{batch.errorSummary}</div>}
              </td>
              <td>{batch.format}</td>
              <td>{batch.status}</td>
              <td>{batch.recordCount}</td>
              <td>{batch.parsedCount}</td>
              <td>{batch.duplicateCount}</td>
              <td>{batch.failedCount}</td>
              <td className={batch.countsReconcile ? undefined : 'error'}>
                {batch.countsReconcile ? 'yes' : 'no'}
              </td>
              <td>
                {canManage && batch.status === 'Failed' && (
                  <button disabled={retry.isPending} onClick={() => retry.mutate(batch.id)} type="button">
                    Retry
                  </button>
                )}
                {canManage && (batch.status === 'Received' || batch.status === 'Parsing') && (
                  <button disabled={cancel.isPending} onClick={() => cancel.mutate(batch.id)} type="button">
                    Cancel
                  </button>
                )}
                {canRunValidation && batch.status === 'Parsed' && (
                  <button disabled={validate.isPending} onClick={() => validate.mutate(batch.id)} type="button">
                    Validate
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {(retry.isError || cancel.isError || validate.isError) && (
        <p className="error">{(retry.error ?? cancel.error ?? validate.error)?.message}</p>
      )}
      {validate.data && (
        <p className="muted">
          Validation run completed: future readiness {validate.data.futureReadinessPercent.toFixed(1)}%,{' '}
          {validate.data.paymentsAtRisk} payments at risk. See Validation.
        </p>
      )}

      {batches.data && (
        <p className="pagination">
          <button disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">
            Previous
          </button>
          Page {batches.data.page} of {Math.max(batches.data.totalPages, 1)}
          <button disabled={page >= batches.data.totalPages} onClick={() => setPage(page + 1)} type="button">
            Next
          </button>
        </p>
      )}

      {selected && (
        <div className="card">
          <h2>Parsed records</h2>
          <label>
            Duplicates only{' '}
            <input
              checked={duplicatesOnly}
              onChange={(event) => {
                setRecordPage(1);
                setDuplicatesOnly(event.target.checked);
              }}
              type="checkbox"
            />
          </label>
          {records.isError && <p className="error">{records.error.message}</p>}
          <table className="table">
            <thead>
              <tr>
                <th>#</th>
                <th>Message</th>
                <th>Role</th>
                <th>Party</th>
                <th>Country</th>
                <th>Town</th>
                <th>Post code</th>
                <th>Street</th>
                <th>Lines</th>
                <th>Duplicate</th>
              </tr>
            </thead>
            <tbody>
              {records.data?.items.map((record) => (
                <tr key={record.id}>
                  <td>{record.sequence}</td>
                  <td className="muted">{record.messageId ?? '—'}</td>
                  <td>{record.partyRole}</td>
                  <td>{record.partyName ?? '—'}</td>
                  <td>{record.country ?? '—'}</td>
                  <td>{record.townName ?? '—'}</td>
                  <td>{record.postCode ?? '—'}</td>
                  <td>
                    {record.streetName ?? '—'} {record.buildingNumber ?? ''}
                  </td>
                  <td className="muted">{record.addressLines ?? '—'}</td>
                  <td>{record.isDuplicate ? 'yes' : 'no'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {records.data && (
            <p className="pagination">
              <button disabled={recordPage <= 1} onClick={() => setRecordPage(recordPage - 1)} type="button">
                Previous
              </button>
              Page {records.data.page} of {Math.max(records.data.totalPages, 1)}
              <button
                disabled={recordPage >= records.data.totalPages}
                onClick={() => setRecordPage(recordPage + 1)}
                type="button"
              >
                Next
              </button>
            </p>
          )}
        </div>
      )}
    </section>
  );
}
