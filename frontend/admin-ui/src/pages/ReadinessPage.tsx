import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import {
  classifications,
  getAssessments,
  getProfile,
  getReadiness,
  getRuns,
  profileDimensions,
  recordOutcomes,
  type AddressClassification,
  type AssessmentFilter,
  type ProfileDimension,
  type RecordOutcome,
  type RuleMode,
} from '../api/validation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

export function ReadinessPage() {
  const canDrillDown = hasPermission('validation.drilldown');
  const [dimension, setDimension] = useState<ProfileDimension>('Scheme');
  const [runPage, setRunPage] = useState(1);
  const [selectedRun, setSelectedRun] = useState<string | null>(null);
  const [assessmentPage, setAssessmentPage] = useState(1);
  const [filter, setFilter] = useState<AssessmentFilter>({ mode: 'Future' });

  const readiness = useQuery({ queryKey: ['validation-readiness'], queryFn: getReadiness });
  const profile = useQuery({ queryKey: ['validation-profile', dimension], queryFn: () => getProfile(dimension) });
  const runs = useQuery({ queryKey: ['validation-runs', runPage], queryFn: () => getRuns(runPage) });
  const assessments = useQuery({
    queryKey: ['validation-assessments', selectedRun, assessmentPage, filter],
    queryFn: () => getAssessments(selectedRun!, assessmentPage, filter),
    enabled: selectedRun !== null,
  });

  const updateFilter = (patch: Partial<AssessmentFilter>) => {
    setAssessmentPage(1);
    setFilter({ ...filter, ...patch });
  };

  return (
    <section>
      <h1>Portfolio readiness</h1>
      <p className="muted">
        How much of the assessed payment-party data passes validation today, and how much would fail once
        structured addresses become mandatory.
      </p>

      {readiness.isError && <p className="error">Readiness could not be loaded: {readiness.error.message}</p>}
      {readiness.data && (
        <div className="metrics">
          <Metric label="Readiness today" value={`${readiness.data.currentReadinessPercent.toFixed(1)}%`} />
          <Metric label="Readiness after cutover" value={`${readiness.data.futureReadinessPercent.toFixed(1)}%`} />
          <Metric label="Payments at risk" value={readiness.data.paymentsAtRisk.toLocaleString()} tone="risk" />
          <Metric label="Records assessed" value={readiness.data.assessedCount.toLocaleString()} />
          <Metric label="Rejected (future)" value={readiness.data.futureRejectedCount.toLocaleString()} tone="risk" />
          <Metric label="Unable to assess" value={readiness.data.unableToAssessCount.toLocaleString()} />
        </div>
      )}

      {readiness.data && readiness.data.topIssues.length > 0 && (
        <div className="card">
          <h2>Top issues</h2>
          <table className="table">
            <thead>
              <tr>
                <th>Rule</th>
                <th>Field</th>
                <th>Severity</th>
                <th>Mode</th>
                <th>Records</th>
              </tr>
            </thead>
            <tbody>
              {readiness.data.topIssues.map((issue) => (
                <tr key={`${issue.ruleCode}-${issue.field}-${issue.mode}`}>
                  <td>{issue.ruleCode}</td>
                  <td>{issue.field}</td>
                  <td>{issue.severity}</td>
                  <td>{issue.mode}</td>
                  <td>{issue.count.toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="card">
        <h2>Exposure profile</h2>
        <p className="muted">
          Counts cover the assessed records of the latest run per batch — excluded and unassessable records
          are left out. Readiness is the compliant share, so a warning counts as neither compliant nor
          rejected. Under the Issue dimension a record appears under every rule it breached, so rows overlap.
        </p>
        <label>
          Dimension{' '}
          <select onChange={(event) => setDimension(event.target.value as ProfileDimension)} value={dimension}>
            {profileDimensions.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
        {profile.isError && <p className="error">{profile.error.message}</p>}
        <table className="table">
          <thead>
            <tr>
              <th>{dimension}</th>
              <th>Records assessed</th>
              <th>Rejected today</th>
              <th>Rejected after cutover</th>
              <th>Warnings today</th>
              <th>Warnings after cutover</th>
              <th>Readiness today</th>
              <th>Readiness after cutover</th>
            </tr>
          </thead>
          <tbody>
            {profile.data?.rows.map((row) => (
              <tr key={row.key}>
                <td>{row.key}</td>
                <td>{row.recordCount.toLocaleString()}</td>
                <td>{row.currentRejectedCount.toLocaleString()}</td>
                <td>{row.futureRejectedCount.toLocaleString()}</td>
                <td>{row.currentWarningCount.toLocaleString()}</td>
                <td>{row.futureWarningCount.toLocaleString()}</td>
                <td>{row.currentReadinessPercent.toFixed(1)}%</td>
                <td>{row.futureReadinessPercent.toFixed(1)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <h2>Validation runs</h2>
      {runs.isError && <p className="error">{runs.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Started</th>
            <th>Source</th>
            <th>Scheme</th>
            <th>As of</th>
            <th>Status</th>
            <th>Assessed</th>
            <th>Readiness today</th>
            <th>After cutover</th>
            <th>At risk</th>
            <th>Reconciles</th>
          </tr>
        </thead>
        <tbody>
          {runs.data?.items.map((run) => (
            <tr key={run.id}>
              <td>
                <button
                  className="link"
                  onClick={() => {
                    setAssessmentPage(1);
                    setSelectedRun(selectedRun === run.id ? null : run.id);
                  }}
                  type="button"
                >
                  {run.startedAtUtc.replace('T', ' ').slice(0, 19)}
                </button>
              </td>
              <td>{run.sourceCode}</td>
              <td>{run.schemeCode}</td>
              <td>{run.asOf}</td>
              <td className={run.status === 'Failed' ? 'error' : undefined}>
                {run.status}
                {run.errorSummary ? `: ${run.errorSummary}` : ''}
              </td>
              <td>{run.assessedCount}</td>
              <td>{run.currentReadinessPercent.toFixed(1)}%</td>
              <td>{run.futureReadinessPercent.toFixed(1)}%</td>
              <td>{run.paymentsAtRisk}</td>
              <td className={run.countsReconcile ? undefined : 'error'}>{run.countsReconcile ? 'yes' : 'no'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {runs.data && (
        <p className="pagination">
          <button disabled={runPage <= 1} onClick={() => setRunPage(runPage - 1)} type="button">
            Previous
          </button>
          Page {runs.data.page} of {Math.max(runs.data.totalPages, 1)}
          <button disabled={runPage >= runs.data.totalPages} onClick={() => setRunPage(runPage + 1)} type="button">
            Next
          </button>
        </p>
      )}

      {selectedRun && (
        <div className="card">
          <h2>Assessed records</h2>
          <p className="muted">
            {canDrillDown
              ? 'You may see full address detail; every drill-down is audited.'
              : 'Address detail is masked — validation.drilldown is required to see full records.'}
          </p>
          <div className="filters">
            <label>
              Rule mode{' '}
              <select
                onChange={(event) => updateFilter({ mode: event.target.value as RuleMode })}
                value={filter.mode}
              >
                <option value="Current">Current</option>
                <option value="Future">Future</option>
              </select>
            </label>
            <label>
              Outcome{' '}
              <select
                onChange={(event) => updateFilter({ outcome: event.target.value as RecordOutcome | '' })}
                value={filter.outcome ?? ''}
              >
                <option value="">Any</option>
                {recordOutcomes.map((outcome) => (
                  <option key={outcome} value={outcome}>
                    {outcome}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Classification{' '}
              <select
                onChange={(event) =>
                  updateFilter({ classification: event.target.value as AddressClassification | '' })
                }
                value={filter.classification ?? ''}
              >
                <option value="">Any</option>
                {classifications.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Rule code{' '}
              <input onChange={(event) => updateFilter({ ruleCode: event.target.value })} value={filter.ruleCode ?? ''} />
            </label>
          </div>

          {assessments.isError && <p className="error">{assessments.error.message}</p>}
          <table className="table">
            <thead>
              <tr>
                <th>#</th>
                <th>Role</th>
                <th>Party</th>
                <th>Address</th>
                <th>Class</th>
                <th>Today</th>
                <th>After cutover</th>
                <th>Findings</th>
                <th>Evidence</th>
              </tr>
            </thead>
            <tbody>
              {assessments.data?.items.map((assessment) => (
                <tr key={assessment.id}>
                  <td>{assessment.sequence}</td>
                  <td>{assessment.partyRole}</td>
                  <td>{assessment.partyName ?? '—'}</td>
                  <td>
                    {[
                      assessment.streetName,
                      assessment.buildingNumber,
                      assessment.postCode,
                      assessment.townName,
                      assessment.country,
                      assessment.addressLines,
                    ]
                      .filter(Boolean)
                      .join(', ') || '—'}
                  </td>
                  <td>{assessment.classification}</td>
                  <td>{assessment.currentOutcome}</td>
                  <td className={assessment.futureOutcome === 'Rejected' ? 'error' : undefined}>
                    {assessment.futureOutcome}
                  </td>
                  <td>
                    {assessment.issues.length === 0
                      ? '—'
                      : assessment.issues.map((issue) => (
                          <div key={issue.id}>
                            <strong>{issue.ruleCode}</strong> {issue.field} — {issue.message}
                            {issue.expected && <span className="muted"> (expected {issue.expected})</span>}
                          </div>
                        ))}
                  </td>
                  <td className="muted">{assessment.evidencePointer}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {assessments.data && (
            <p className="pagination">
              <button
                disabled={assessmentPage <= 1}
                onClick={() => setAssessmentPage(assessmentPage - 1)}
                type="button"
              >
                Previous
              </button>
              Page {assessments.data.page} of {Math.max(assessments.data.totalPages, 1)}
              <button
                disabled={assessmentPage >= assessments.data.totalPages}
                onClick={() => setAssessmentPage(assessmentPage + 1)}
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
