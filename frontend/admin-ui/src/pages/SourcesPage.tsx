import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  attestSource,
  getSourceReadiness,
  getSources,
  onboardingStatuses,
  recordScan,
  type SourceFilter,
} from '../api/sources';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

export function SourcesPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('sources.write');
  const canAttest = hasPermission('sources.attest');
  const [filter, setFilter] = useState<SourceFilter>({});
  const [expanded, setExpanded] = useState<string | null>(null);
  const [coverage, setCoverage] = useState<Record<string, string>>({});

  const sources = useQuery({ queryKey: ['sources', filter], queryFn: () => getSources(filter) });
  const readiness = useQuery({ queryKey: ['sources-readiness'], queryFn: getSourceReadiness });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['sources'] });
    void queryClient.invalidateQueries({ queryKey: ['sources-readiness'] });
  };

  const attest = useMutation({ mutationFn: attestSource, onSuccess: invalidate });
  const scan = useMutation({
    mutationFn: ({ code, percent }: { code: string; percent: number }) => recordScan(code, percent),
    onSuccess: invalidate,
  });

  return (
    <section>
      <h1>Source systems</h1>
      <p className="muted">
        Every system that holds payment-party addresses, with its ISO 20022 mapping, lineage, scan coverage and
        owner attestation.
      </p>

      {readiness.data && (
        <div className="metrics">
          <Metric label="Sources" value={readiness.data.totalSources} />
          <Metric label="Ready" value={readiness.data.readySources} />
          <Metric label="Blocked" value={readiness.data.blockedSources} />
          <Metric label="Attestation overdue" value={readiness.data.attestationOverdueSources} />
          <Metric
            label="Parties covered"
            value={`${readiness.data.coveredPartyCount.toLocaleString()} / ${readiness.data.totalPartyCount.toLocaleString()}`}
          />
          <Metric label="Avg readiness" value={`${readiness.data.averageReadinessScore.toFixed(1)}%`} />
        </div>
      )}

      <div className="filters">
        <label>
          Scheme{' '}
          <input
            onChange={(event) => setFilter({ ...filter, schemeCode: event.target.value })}
            value={filter.schemeCode ?? ''}
          />
        </label>
        <label>
          Status{' '}
          <select
            onChange={(event) => setFilter({ ...filter, status: event.target.value as SourceFilter['status'] })}
            value={filter.status ?? ''}
          >
            <option value="">Any</option>
            {onboardingStatuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
        <label>
          Attestation overdue only{' '}
          <input
            checked={filter.attestationOverdueOnly ?? false}
            onChange={(event) => setFilter({ ...filter, attestationOverdueOnly: event.target.checked })}
            type="checkbox"
          />
        </label>
      </div>

      {sources.isPending && <p>Loading…</p>}
      {sources.isError && <p className="error">Sources could not be loaded: {sources.error.message}</p>}

      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Owner</th>
            <th>Schemes</th>
            <th>Status</th>
            <th>Mapping</th>
            <th>Coverage</th>
            <th>Readiness</th>
            <th>Attested</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {sources.data?.map((source) => (
            <tr key={source.id}>
              <td>
                <button className="link" onClick={() => setExpanded(expanded === source.code ? null : source.code)} type="button">
                  {source.code}
                </button>
              </td>
              <td>{source.name}</td>
              <td>
                {source.ownerName} <span className="muted">{source.ownerEmail}</span>
              </td>
              <td>{source.schemeCodes.join(', ')}</td>
              <td>{source.status}</td>
              <td>{source.mapping}</td>
              <td>{source.scanCoveragePercent.toFixed(1)}%</td>
              <td>{source.readinessScore.toFixed(1)}%</td>
              <td className={source.attestationOverdue ? 'error' : undefined}>
                {source.lastAttestedAtUtc ? source.lastAttestedAtUtc.slice(0, 10) : 'never'}
                {source.attestationOverdue && ' (overdue)'}
              </td>
              <td>
                {canAttest && (
                  <button disabled={attest.isPending} onClick={() => attest.mutate(source.code)} type="button">
                    Attest
                  </button>
                )}{' '}
                {canWrite && (
                  <>
                    <input
                      aria-label={`Scan coverage for ${source.code}`}
                      onChange={(event) => setCoverage({ ...coverage, [source.code]: event.target.value })}
                      size={4}
                      value={coverage[source.code] ?? ''}
                    />
                    <button
                      disabled={scan.isPending}
                      onClick={() =>
                        scan.mutate({ code: source.code, percent: Number(coverage[source.code] ?? '0') })
                      }
                      type="button"
                    >
                      Record scan
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {(attest.isError || scan.isError) && (
        <p className="error">{(attest.error ?? scan.error)?.message}</p>
      )}

      {expanded &&
        sources.data
          ?.filter((source) => source.code === expanded)
          .map((source) => (
            <div className="card" key={source.id}>
              <h2>{source.code} — field mappings</h2>
              <table className="table">
                <thead>
                  <tr>
                    <th>Source attribute</th>
                    <th>ISO 20022 element</th>
                    <th>Transformation</th>
                    <th>Authoritative</th>
                    <th>Reviewed</th>
                  </tr>
                </thead>
                <tbody>
                  {source.mappings.map((mapping) => (
                    <tr key={mapping.id}>
                      <td>{mapping.sourceAttribute}</td>
                      <td>{mapping.targetElement}</td>
                      <td className="muted">{mapping.transformation ?? '—'}</td>
                      <td>{mapping.isAuthoritative ? 'yes' : 'no'}</td>
                      <td className="muted">{mapping.lastReviewedAtUtc?.slice(0, 10) ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <h2>Lineage</h2>
              <ol>
                {source.lineage.map((step) => (
                  <li key={step.sequence}>
                    {step.fromNode} → {step.toNode} <span className="muted">{step.channel ?? ''} {step.description ?? ''}</span>
                  </li>
                ))}
              </ol>
            </div>
          ))}
    </section>
  );
}
