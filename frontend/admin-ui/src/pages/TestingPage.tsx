import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  activateTestPlan,
  addTestCase,
  closeTestPlan,
  createTestPlan,
  executionStatuses,
  getTestPlan,
  getTestPlans,
  recordExecution,
  recordUat,
  testRisks,
  type TestExecutionStatus,
  type TestRisk,
} from '../api/simulation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

export function TestingPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('testing.write');

  const [selected, setSelected] = useState<string | null>(null);
  const [planDraft, setPlanDraft] = useState({ code: '', name: '', owner: '', scope: '' });
  const [caseDraft, setCaseDraft] = useState({
    reference: '',
    title: '',
    risk: 'High' as TestRisk,
    scenarioCode: '',
    sampleReference: '',
    expectedResult: '',
  });
  const [execution, setExecution] = useState({
    reference: '',
    status: 'Passed' as TestExecutionStatus,
    actualResult: '',
    evidenceReference: '',
    defectReference: '',
  });
  const [uat, setUat] = useState({ reference: '', engineOutcome: '', platformOutcome: '', explanation: '' });

  const plans = useQuery({ queryKey: ['test-plans'], queryFn: getTestPlans });
  const plan = useQuery({
    queryKey: ['test-plan', selected],
    queryFn: () => getTestPlan(selected!),
    enabled: selected !== null,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['test-plans'] });
    void queryClient.invalidateQueries({ queryKey: ['test-plan'] });
  };

  const create = useMutation({
    mutationFn: () =>
      createTestPlan({
        code: planDraft.code,
        name: planDraft.name,
        owner: planDraft.owner,
        scope: planDraft.scope || null,
      }),
    onSuccess: (created) => {
      setSelected(created?.code ?? null);
      invalidate();
    },
  });
  const activate = useMutation({ mutationFn: activateTestPlan, onSuccess: invalidate });
  const close = useMutation({ mutationFn: closeTestPlan, onSuccess: invalidate });
  const addCase = useMutation({
    mutationFn: () =>
      addTestCase(selected!, {
        reference: caseDraft.reference,
        title: caseDraft.title,
        risk: caseDraft.risk,
        scenarioCode: caseDraft.scenarioCode || null,
        sampleReference: caseDraft.sampleReference || null,
        expectedResult: caseDraft.expectedResult,
      }),
    onSuccess: () => {
      setCaseDraft({ ...caseDraft, reference: '', title: '', expectedResult: '' });
      invalidate();
    },
  });
  const record = useMutation({
    mutationFn: () =>
      recordExecution(selected!, execution.reference, {
        status: execution.status,
        actualResult: execution.actualResult,
        evidenceReference: execution.evidenceReference || null,
        defectReference: execution.defectReference || null,
      }),
    onSuccess: invalidate,
  });
  const reconcile = useMutation({
    mutationFn: () =>
      recordUat(selected!, uat.reference, {
        engineOutcome: uat.engineOutcome,
        platformOutcome: uat.platformOutcome,
        explanation: uat.explanation || null,
      }),
    onSuccess: invalidate,
  });

  return (
    <section>
      <h1>Testing</h1>
      <p className="muted">
        Risk-based plans: what is exercised, what was expected, what actually happened, and whether the
        payment engine agreed with the platform on the same sample.
      </p>

      {plans.isError && <p className="error">{plans.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Owner</th>
            <th>Status</th>
            <th>Cases</th>
            <th>Passed</th>
            <th>Failed</th>
            <th>Open defects</th>
            <th>UAT mismatches</th>
            <th>Risk-weighted coverage</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {plans.data?.map((item) => (
            <tr key={item.id}>
              <td>
                <button className="link" onClick={() => setSelected(item.code)} type="button">
                  {item.code}
                </button>
              </td>
              <td>{item.name}</td>
              <td>{item.owner}</td>
              <td>{item.status}</td>
              <td>{item.caseCount}</td>
              <td>{item.passedCount}</td>
              <td className={item.failedCount > 0 ? 'error' : undefined}>{item.failedCount}</td>
              <td className={item.openDefectCount > 0 ? 'error' : undefined}>{item.openDefectCount}</td>
              <td className={item.uatMismatchCount > 0 ? 'error' : undefined}>{item.uatMismatchCount}</td>
              <td>{item.riskWeightedCoveragePercent.toFixed(1)}%</td>
              <td>
                {canWrite && item.status === 'Draft' && (
                  <button onClick={() => activate.mutate(item.code)} type="button">
                    Activate
                  </button>
                )}
                {canWrite && item.status === 'Active' && (
                  <button onClick={() => close.mutate(item.code)} type="button">
                    Close
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {canWrite && (
        <div className="card">
          <h2>New plan</h2>
          <div className="filters">
            <label>
              Code
              <input
                onChange={(event) => setPlanDraft({ ...planDraft, code: event.target.value.toUpperCase() })}
                value={planDraft.code}
              />
            </label>
            <label>
              Name
              <input onChange={(event) => setPlanDraft({ ...planDraft, name: event.target.value })} value={planDraft.name} />
            </label>
            <label>
              Owner
              <input onChange={(event) => setPlanDraft({ ...planDraft, owner: event.target.value })} value={planDraft.owner} />
            </label>
            <label>
              Scope
              <input onChange={(event) => setPlanDraft({ ...planDraft, scope: event.target.value })} value={planDraft.scope} />
            </label>
            <button
              disabled={!planDraft.code || !planDraft.name || !planDraft.owner || create.isPending}
              onClick={() => create.mutate()}
              type="button"
            >
              Create
            </button>
          </div>
          {create.isError && <p className="error">{create.error.message}</p>}
        </div>
      )}

      {plan.data && (
        <>
          <h2>
            {plan.data.code} — {plan.data.name}
          </h2>
          <div className="metrics">
            <Metric label="Cases" value={plan.data.caseCount} />
            <Metric label="Not run" value={plan.data.notRunCount} />
            <Metric label="Blocked" value={plan.data.blockedCount} />
            <Metric label="Coverage" value={`${plan.data.riskWeightedCoveragePercent.toFixed(1)}%`} />
          </div>
          <table className="table">
            <thead>
              <tr>
                <th>Reference</th>
                <th>Title</th>
                <th>Risk</th>
                <th>Scenario</th>
                <th>Expected</th>
                <th>Status</th>
                <th>Actual</th>
                <th>Defect</th>
                <th>Retested</th>
                <th>UAT</th>
                <th>Engine / platform</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {plan.data.cases.map((testCase) => (
                <tr key={testCase.id}>
                  <td>{testCase.reference}</td>
                  <td>{testCase.title}</td>
                  <td>{testCase.risk}</td>
                  <td>{testCase.scenarioCode ?? '—'}</td>
                  <td>{testCase.expectedResult}</td>
                  <td className={testCase.status === 'Failed' ? 'error' : undefined}>{testCase.status}</td>
                  <td>{testCase.actualResult ?? '—'}</td>
                  <td>{testCase.defectReference ?? '—'}</td>
                  <td>{testCase.isRetested ? 'yes' : 'no'}</td>
                  <td className={testCase.uatOutcome === 'Mismatch' ? 'error' : undefined}>{testCase.uatOutcome}</td>
                  <td>
                    {testCase.engineOutcome ?? '—'} / {testCase.platformOutcome ?? '—'}
                  </td>
                  <td>
                    {canWrite && (
                      <>
                        <button
                          onClick={() => setExecution({ ...execution, reference: testCase.reference })}
                          type="button"
                        >
                          Execute
                        </button>{' '}
                        <button onClick={() => setUat({ ...uat, reference: testCase.reference })} type="button">
                          UAT
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {canWrite && (
            <>
              <div className="card">
                <h3>Add case</h3>
                <div className="filters">
                  <label>
                    Reference
                    <input
                      onChange={(event) => setCaseDraft({ ...caseDraft, reference: event.target.value.toUpperCase() })}
                      value={caseDraft.reference}
                    />
                  </label>
                  <label>
                    Title
                    <input onChange={(event) => setCaseDraft({ ...caseDraft, title: event.target.value })} value={caseDraft.title} />
                  </label>
                  <label>
                    Risk
                    <select
                      onChange={(event) => setCaseDraft({ ...caseDraft, risk: event.target.value as TestRisk })}
                      value={caseDraft.risk}
                    >
                      {testRisks.map((risk) => (
                        <option key={risk} value={risk}>
                          {risk}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Scenario
                    <input
                      onChange={(event) => setCaseDraft({ ...caseDraft, scenarioCode: event.target.value.toUpperCase() })}
                      value={caseDraft.scenarioCode}
                    />
                  </label>
                  <label>
                    Sample
                    <input
                      onChange={(event) => setCaseDraft({ ...caseDraft, sampleReference: event.target.value })}
                      value={caseDraft.sampleReference}
                    />
                  </label>
                  <label>
                    Expected
                    <input
                      onChange={(event) => setCaseDraft({ ...caseDraft, expectedResult: event.target.value })}
                      value={caseDraft.expectedResult}
                    />
                  </label>
                  <button
                    disabled={!caseDraft.reference || !caseDraft.title || !caseDraft.expectedResult || addCase.isPending}
                    onClick={() => addCase.mutate()}
                    type="button"
                  >
                    Add
                  </button>
                </div>
                {addCase.isError && <p className="error">{addCase.error.message}</p>}
              </div>

              <div className="card">
                <h3>Record execution</h3>
                <div className="filters">
                  <label>
                    Case
                    <input
                      onChange={(event) => setExecution({ ...execution, reference: event.target.value.toUpperCase() })}
                      value={execution.reference}
                    />
                  </label>
                  <label>
                    Status
                    <select
                      onChange={(event) => setExecution({ ...execution, status: event.target.value as TestExecutionStatus })}
                      value={execution.status}
                    >
                      {executionStatuses.map((status) => (
                        <option key={status} value={status}>
                          {status}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Actual
                    <input
                      onChange={(event) => setExecution({ ...execution, actualResult: event.target.value })}
                      value={execution.actualResult}
                    />
                  </label>
                  <label>
                    Evidence
                    <input
                      onChange={(event) => setExecution({ ...execution, evidenceReference: event.target.value })}
                      value={execution.evidenceReference}
                    />
                  </label>
                  <label>
                    Defect
                    <input
                      onChange={(event) => setExecution({ ...execution, defectReference: event.target.value })}
                      value={execution.defectReference}
                    />
                  </label>
                  <button
                    disabled={!execution.reference || !execution.actualResult || record.isPending}
                    onClick={() => record.mutate()}
                    type="button"
                  >
                    Record
                  </button>
                </div>
                {record.isError && <p className="error">{record.error.message}</p>}
              </div>

              <div className="card">
                <h3>UAT reconciliation</h3>
                <div className="filters">
                  <label>
                    Case
                    <input
                      onChange={(event) => setUat({ ...uat, reference: event.target.value.toUpperCase() })}
                      value={uat.reference}
                    />
                  </label>
                  <label>
                    Engine outcome
                    <input onChange={(event) => setUat({ ...uat, engineOutcome: event.target.value })} value={uat.engineOutcome} />
                  </label>
                  <label>
                    Platform outcome
                    <input
                      onChange={(event) => setUat({ ...uat, platformOutcome: event.target.value })}
                      value={uat.platformOutcome}
                    />
                  </label>
                  <label>
                    Explanation
                    <input onChange={(event) => setUat({ ...uat, explanation: event.target.value })} value={uat.explanation} />
                  </label>
                  <button
                    disabled={!uat.reference || !uat.engineOutcome || !uat.platformOutcome || reconcile.isPending}
                    onClick={() => reconcile.mutate()}
                    type="button"
                  >
                    Reconcile
                  </button>
                </div>
                {reconcile.isError && <p className="error">{reconcile.error.message}</p>}
              </div>
            </>
          )}
        </>
      )}
    </section>
  );
}
