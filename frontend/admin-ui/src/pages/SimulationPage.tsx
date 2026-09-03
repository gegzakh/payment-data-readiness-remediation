import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  archiveScenario,
  compareRuns,
  createScenario,
  getRuns,
  getScenarios,
  lockScenario,
  runScenario,
  scenarioModes,
  type RunComparisonDto,
  type ScenarioMode,
} from '../api/simulation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

const today = () => new Date().toISOString().slice(0, 10);

export function SimulationPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('simulation.write');

  const [scenarioCode, setScenarioCode] = useState('');
  const [page, setPage] = useState(1);
  const [baselineId, setBaselineId] = useState('');
  const [candidateId, setCandidateId] = useState('');
  const [comparison, setComparison] = useState<RunComparisonDto | null>(null);
  const [draft, setDraft] = useState({
    code: '',
    name: '',
    mode: 'Future' as ScenarioMode,
    asOf: today(),
    schemeCodes: '',
    sourceCodes: '',
    exclusions: '',
  });

  const scenarios = useQuery({ queryKey: ['scenarios'], queryFn: getScenarios });
  const runs = useQuery({
    queryKey: ['simulation-runs', scenarioCode, page],
    queryFn: () => getRuns(scenarioCode || undefined, page),
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['scenarios'] });
    void queryClient.invalidateQueries({ queryKey: ['simulation-runs'] });
  };

  const create = useMutation({
    mutationFn: () =>
      createScenario({
        code: draft.code,
        name: draft.name,
        mode: draft.mode,
        asOf: draft.asOf,
        schemeCodes: draft.schemeCodes || null,
        sourceCodes: draft.sourceCodes || null,
        exclusions: draft.exclusions || null,
      }),
    onSuccess: () => {
      setDraft({ ...draft, code: '', name: '' });
      invalidate();
    },
  });
  const lock = useMutation({ mutationFn: lockScenario, onSuccess: invalidate });
  const archive = useMutation({ mutationFn: archiveScenario, onSuccess: invalidate });
  const run = useMutation({ mutationFn: runScenario, onSuccess: invalidate });
  const compare = useMutation({
    mutationFn: () => compareRuns(baselineId, candidateId),
    onSuccess: (result) => setComparison(result),
  });

  return (
    <section>
      <h1>Simulation</h1>
      <p className="muted">
        Scenarios describe a population and a ruleset; every run of a locked scenario is reproducible and
        comparable, so a readiness number can always be traced back to what produced it.
      </p>

      {scenarios.isError && <p className="error">{scenarios.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Mode</th>
            <th>As of</th>
            <th>Scope</th>
            <th>Ruleset</th>
            <th>Status</th>
            <th>Runs</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {scenarios.data?.map((scenario) => (
            <tr key={scenario.id}>
              <td>
                <button className="link" onClick={() => setScenarioCode(scenario.code)} type="button">
                  {scenario.code}
                </button>
              </td>
              <td>{scenario.name}</td>
              <td>{scenario.mode}</td>
              <td>{scenario.asOf}</td>
              <td>
                {[scenario.schemeCodes, scenario.sourceCodes, scenario.countries].filter(Boolean).join(' / ') ||
                  'all'}
              </td>
              <td>{scenario.rulesetVersion ?? 'active'}</td>
              <td>{scenario.status}</td>
              <td>{scenario.runCount}</td>
              <td>
                {canWrite && scenario.status === 'Draft' && (
                  <button onClick={() => lock.mutate(scenario.code)} type="button">
                    Lock
                  </button>
                )}{' '}
                {canWrite && scenario.status !== 'Archived' && (
                  <>
                    <button onClick={() => run.mutate(scenario.code)} type="button">
                      Run
                    </button>{' '}
                    <button onClick={() => archive.mutate(scenario.code)} type="button">
                      Archive
                    </button>
                  </>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {run.isError && <p className="error">{run.error.message}</p>}

      {canWrite && (
        <div className="card">
          <h2>New scenario</h2>
          <div className="filters">
            <label>
              Code
              <input
                onChange={(event) => setDraft({ ...draft, code: event.target.value.toUpperCase() })}
                value={draft.code}
              />
            </label>
            <label>
              Name <input onChange={(event) => setDraft({ ...draft, name: event.target.value })} value={draft.name} />
            </label>
            <label>
              Mode
              <select
                onChange={(event) => setDraft({ ...draft, mode: event.target.value as ScenarioMode })}
                value={draft.mode}
              >
                {scenarioModes.map((mode) => (
                  <option key={mode} value={mode}>
                    {mode}
                  </option>
                ))}
              </select>
            </label>
            <label>
              As of
              <input
                onChange={(event) => setDraft({ ...draft, asOf: event.target.value })}
                type="date"
                value={draft.asOf}
              />
            </label>
            <label>
              Schemes
              <input
                onChange={(event) => setDraft({ ...draft, schemeCodes: event.target.value.toUpperCase() })}
                placeholder="SEPA,SWIFT"
                value={draft.schemeCodes}
              />
            </label>
            <label>
              Sources
              <input
                onChange={(event) => setDraft({ ...draft, sourceCodes: event.target.value.toUpperCase() })}
                placeholder="HUB-EU"
                value={draft.sourceCodes}
              />
            </label>
            <label>
              Exclusions
              <input
                onChange={(event) => setDraft({ ...draft, exclusions: event.target.value.toUpperCase() })}
                placeholder="DORMANT"
                value={draft.exclusions}
              />
            </label>
            <button disabled={!draft.code || !draft.name || create.isPending} onClick={() => create.mutate()} type="button">
              Create
            </button>
          </div>
          {create.isError && <p className="error">{create.error.message}</p>}
        </div>
      )}

      <h2>Runs {scenarioCode && `for ${scenarioCode}`}</h2>
      <div className="filters">
        <label>
          Scenario
          <input
            onChange={(event) => {
              setPage(1);
              setScenarioCode(event.target.value.toUpperCase());
            }}
            placeholder="all"
            value={scenarioCode}
          />
        </label>
      </div>
      {runs.isError && <p className="error">{runs.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Started</th>
            <th>Scenario</th>
            <th>Mode</th>
            <th>Status</th>
            <th>Population</th>
            <th>Rejected</th>
            <th>Payments at risk</th>
            <th>Readiness</th>
            <th>Reconciles</th>
            <th>Run key</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {runs.data?.items.map((item) => (
            <tr key={item.id}>
              <td>{new Date(item.startedAtUtc).toLocaleString()}</td>
              <td>{item.scenarioCode}</td>
              <td>{item.mode}</td>
              <td className={item.status === 'Failed' ? 'error' : undefined}>{item.status}</td>
              <td>{item.populationCount.toLocaleString()}</td>
              <td>{item.rejectedCount.toLocaleString()}</td>
              <td>{item.paymentsAtRisk.toLocaleString()}</td>
              <td>{item.readinessPercent.toFixed(1)}%</td>
              <td className={item.reconciles ? undefined : 'error'}>{item.reconciles ? 'yes' : 'no'}</td>
              <td title={item.runKey}>{item.runKey.slice(0, 12)}…</td>
              <td>
                <button onClick={() => setBaselineId(item.id)} type="button">
                  Baseline
                </button>{' '}
                <button onClick={() => setCandidateId(item.id)} type="button">
                  Candidate
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="pager">
        <button disabled={page === 1} onClick={() => setPage(page - 1)} type="button">
          Previous
        </button>
        <span>Page {page}</span>
        <button disabled={(runs.data?.items.length ?? 0) === 0} onClick={() => setPage(page + 1)} type="button">
          Next
        </button>
      </div>

      <div className="card">
        <h2>Compare runs</h2>
        <div className="filters">
          <label>
            Baseline <input onChange={(event) => setBaselineId(event.target.value)} value={baselineId} />
          </label>
          <label>
            Candidate <input onChange={(event) => setCandidateId(event.target.value)} value={candidateId} />
          </label>
          <button disabled={!baselineId || !candidateId || compare.isPending} onClick={() => compare.mutate()} type="button">
            Compare
          </button>
        </div>
        {compare.isError && <p className="error">{compare.error.message}</p>}
        {comparison && (
          <>
            <div className="metrics">
              <Metric label="Rejected delta" value={comparison.rejectedDelta.toLocaleString()} tone="risk" />
              <Metric label="Payments at risk delta" value={comparison.paymentsAtRiskDelta.toLocaleString()} tone="risk" />
              <Metric label="Readiness delta" value={`${comparison.readinessDelta.toFixed(1)}%`} />
              <Metric label="Same run key" value={comparison.sameRunKey ? 'yes' : 'no'} />
            </div>
            <table className="table">
              <thead>
                <tr>
                  <th>Dimension</th>
                  <th>Key</th>
                  <th>Baseline rejected</th>
                  <th>Candidate rejected</th>
                  <th>Delta</th>
                </tr>
              </thead>
              <tbody>
                {comparison.rows.map((row) => (
                  <tr key={`${row.dimension}-${row.key}`}>
                    <td>{row.dimension}</td>
                    <td>{row.key}</td>
                    <td>{row.baselineRejected.toLocaleString()}</td>
                    <td>{row.candidateRejected.toLocaleString()}</td>
                    <td className={row.rejectedDelta > 0 ? 'error' : undefined}>{row.rejectedDelta}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </>
        )}
      </div>
    </section>
  );
}
