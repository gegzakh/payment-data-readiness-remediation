import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { auditOutcomes, getAuditRecords, verifyAuditChain, type AuditFilter } from '../api/audit';
import { hasPermission } from '../auth/keycloak';

export function AuditPage() {
  const canVerify = hasPermission('audit.verify');
  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<AuditFilter>({});

  const records = useQuery({
    queryKey: ['audit', page, filter],
    queryFn: () => getAuditRecords(page, filter),
  });

  const verification = useQuery({
    queryKey: ['audit-verification'],
    queryFn: verifyAuditChain,
    enabled: false,
  });

  const update = (patch: Partial<AuditFilter>) => {
    setPage(1);
    setFilter({ ...filter, ...patch });
  };

  return (
    <section>
      <h1>Audit ledger</h1>
      <p className="muted">
        Append-only evidence: every record is hash-chained to its predecessor, so edits made behind the
        application&apos;s back break verification.
      </p>

      <div className="filters">
        <label>
          Service <input onChange={(event) => update({ service: event.target.value })} value={filter.service ?? ''} />
        </label>
        <label>
          Action <input onChange={(event) => update({ action: event.target.value })} value={filter.action ?? ''} />
        </label>
        <label>
          Entity type{' '}
          <input onChange={(event) => update({ entityType: event.target.value })} value={filter.entityType ?? ''} />
        </label>
        <label>
          Actor <input onChange={(event) => update({ actor: event.target.value })} value={filter.actor ?? ''} />
        </label>
        <label>
          Outcome{' '}
          <select
            onChange={(event) => update({ outcome: event.target.value as AuditFilter['outcome'] })}
            value={filter.outcome ?? ''}
          >
            <option value="">Any</option>
            {auditOutcomes.map((outcome) => (
              <option key={outcome} value={outcome}>
                {outcome}
              </option>
            ))}
          </select>
        </label>
      </div>

      {canVerify && (
        <p>
          <button onClick={() => void verification.refetch()} type="button">
            Verify chain integrity
          </button>{' '}
          {verification.isFetching && <span>Verifying…</span>}
          {verification.data && (
            <span className={verification.data.isIntact ? 'muted' : 'error'}>
              {verification.data.isIntact
                ? `Intact — ${verification.data.recordsChecked} records verified`
                : `Broken at sequence ${verification.data.firstBrokenSequence}`}
            </span>
          )}
          {verification.isError && <span className="error">{verification.error.message}</span>}
        </p>
      )}

      {records.isPending && <p>Loading…</p>}
      {records.isError && <p className="error">Audit records could not be loaded: {records.error.message}</p>}

      <table className="table">
        <thead>
          <tr>
            <th>#</th>
            <th>When (UTC)</th>
            <th>Service</th>
            <th>Action</th>
            <th>Entity</th>
            <th>Actor</th>
            <th>Outcome</th>
            <th>Hash</th>
          </tr>
        </thead>
        <tbody>
          {records.data?.items.map((record) => (
            <tr key={record.id}>
              <td>{record.sequence}</td>
              <td>{new Date(record.occurredAtUtc).toISOString().replace('T', ' ').slice(0, 19)}</td>
              <td>{record.service}</td>
              <td>{record.action}</td>
              <td>
                {record.entityType} <span className="muted">{record.entityId}</span>
              </td>
              <td>{record.actor}</td>
              <td>{record.outcome}</td>
              <td className="muted">{record.hash.slice(0, 12)}…</td>
            </tr>
          ))}
        </tbody>
      </table>

      {records.data && (
        <p>
          <button disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">
            Previous
          </button>{' '}
          Page {records.data.page} of {Math.max(records.data.totalPages, 1)}{' '}
          <button disabled={page >= records.data.totalPages} onClick={() => setPage(page + 1)} type="button">
            Next
          </button>
        </p>
      )}
    </section>
  );
}
