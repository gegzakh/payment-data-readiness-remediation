import { useMutation, useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import {
  dashboardAudiences,
  drillDown,
  exportDashboard,
  getDashboard,
  type DashboardAudience,
  type DashboardScope,
  type DrillDownDto,
  type MetricDto,
} from '../api/reporting';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

function formatMetric(metric: MetricDto): string {
  if (metric.text) {
    return metric.text;
  }

  switch (metric.unit) {
    case 'Percent':
      return `${metric.value.toFixed(1)}%`;
    case 'Money':
      return metric.value.toLocaleString(undefined, { maximumFractionDigits: 0 });
    default:
      return metric.value.toLocaleString();
  }
}

export function DashboardsPage() {
  const canExport = hasPermission('reporting.export');

  const [audience, setAudience] = useState<DashboardAudience>('Executive');
  const [scope, setScope] = useState<DashboardScope>({});
  const [drill, setDrill] = useState<DrillDownDto | null>(null);

  const dashboard = useQuery({
    queryKey: ['dashboard', audience, scope],
    queryFn: () => getDashboard(audience, scope),
  });

  const refresh = useMutation({ mutationFn: () => getDashboard(audience, scope, true) });
  const openDrill = useMutation({
    mutationFn: (dimension: string) => drillDown(audience, dimension, scope),
    onSuccess: (result) => setDrill(result),
  });
  const download = useMutation({ mutationFn: () => exportDashboard(audience, scope) });

  const updateScope = (patch: Partial<DashboardScope>) => {
    setDrill(null);
    setScope({ ...scope, ...patch });
  };

  const snapshot = refresh.data ?? dashboard.data;

  return (
    <section>
      <h1>Dashboards</h1>
      <p className="muted">
        Every figure is stamped with its scope, exclusions, ruleset and freshness, and reconciles against
        the sources behind it — so two people reading the same dashboard read the same number.
      </p>

      <div className="filters">
        <label>
          Audience
          <select
            onChange={(event) => {
              setDrill(null);
              setAudience(event.target.value as DashboardAudience);
            }}
            value={audience}
          >
            {dashboardAudiences.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>
        <label>
          Schemes
          <input
            onChange={(event) => updateScope({ schemeCodes: event.target.value.toUpperCase() })}
            placeholder="SEPA,SWIFT"
            value={scope.schemeCodes ?? ''}
          />
        </label>
        <label>
          Sources
          <input
            onChange={(event) => updateScope({ sourceCodes: event.target.value.toUpperCase() })}
            placeholder="HUB-EU"
            value={scope.sourceCodes ?? ''}
          />
        </label>
        <label>
          Countries
          <input
            onChange={(event) => updateScope({ countries: event.target.value.toUpperCase() })}
            placeholder="DE,FR"
            value={scope.countries ?? ''}
          />
        </label>
        <label>
          Exclusions
          <input
            onChange={(event) => updateScope({ exclusions: event.target.value.toUpperCase() })}
            placeholder="DORMANT"
            value={scope.exclusions ?? ''}
          />
        </label>
        <label>
          As of
          <input onChange={(event) => updateScope({ asOf: event.target.value })} type="date" value={scope.asOf ?? ''} />
        </label>
        <button disabled={refresh.isPending} onClick={() => refresh.mutate()} type="button">
          Refresh
        </button>
        {canExport && (
          <button disabled={download.isPending} onClick={() => download.mutate()} type="button">
            Export CSV
          </button>
        )}
      </div>

      {dashboard.isError && <p className="error">{dashboard.error.message}</p>}
      {download.isError && <p className="error">Export failed: {download.error.message}</p>}

      {snapshot && (
        <>
          <p className="muted">
            {snapshot.scopeDescription} · captured {new Date(snapshot.capturedAtUtc).toLocaleString()} · source as of{' '}
            {snapshot.sourceAsOfUtc ? new Date(snapshot.sourceAsOfUtc).toLocaleString() : 'unknown'} · ruleset{' '}
            {snapshot.rulesetVersion ?? 'unknown'} ·{' '}
            <span className={snapshot.reconciliation === 'Reconciled' ? undefined : 'error'}>
              {snapshot.reconciliation}
              {snapshot.reconciliationNote ? `: ${snapshot.reconciliationNote}` : ''}
            </span>
          </p>

          <div className="metrics">
            {snapshot.metrics.map((metric) => (
              <Metric
                key={metric.key}
                label={metric.label}
                tone={metric.direction === 'LowerIsBetter' && metric.value > 0 ? 'risk' : undefined}
                value={formatMetric(metric)}
              />
            ))}
          </div>

          <h2>Drill down</h2>
          <div className="filters">
            {[...new Set(snapshot.metrics.map((metric) => metric.drillDimension).filter(Boolean))].map((dimension) => (
              <button key={dimension} onClick={() => openDrill.mutate(dimension!)} type="button">
                {dimension}
              </button>
            ))}
          </div>
          {openDrill.isError && <p className="error">{openDrill.error.message}</p>}

          <table className="table">
            <thead>
              <tr>
                <th>Dimension</th>
                <th>Key</th>
                <th>Records</th>
                <th>Rejected</th>
                <th>Warnings</th>
                <th>Payments at risk</th>
                <th>Readiness</th>
              </tr>
            </thead>
            <tbody>
              {(drill?.rows ?? snapshot.breakdown).map((row) => (
                <tr key={`${row.dimension}-${row.key}`}>
                  <td>{row.dimension}</td>
                  <td>{row.key}</td>
                  <td>{row.recordCount.toLocaleString()}</td>
                  <td>{row.rejectedCount.toLocaleString()}</td>
                  <td>{row.warningCount.toLocaleString()}</td>
                  <td>{row.paymentsAtRisk.toLocaleString()}</td>
                  <td>{row.readinessPercent.toFixed(1)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </section>
  );
}
