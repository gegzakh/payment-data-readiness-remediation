import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  addCriterion,
  approveCutover,
  createCutoverPlan,
  criterionStatuses,
  getCutoverPlans,
  getGoNoGoPack,
  recordCriterion,
  setOperationalPlan,
  type ApprovalDecision,
  type CriterionKind,
  type CriterionStatus,
} from '../api/simulation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

export function CutoverPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('cutover.write');
  const canApprove = hasPermission('cutover.approve');

  const [selected, setSelected] = useState<string | null>(null);
  const [planDraft, setPlanDraft] = useState({ code: '', name: '', cutoverDate: '', owner: '' });
  const [operations, setOperations] = useState({ freezeFrom: '', freezeTo: '', fallbackPlan: '', supportModel: '' });
  const [criterion, setCriterion] = useState({
    reference: '',
    kind: 'Entry' as CriterionKind,
    description: '',
    owner: '',
    isBlocking: true,
  });
  const [status, setStatus] = useState({
    reference: '',
    status: 'Met' as CriterionStatus,
    evidenceReference: '',
    rationale: '',
  });
  const [approval, setApproval] = useState({ role: '', decision: 'Approved' as ApprovalDecision, rationale: '' });

  const plans = useQuery({ queryKey: ['cutover-plans'], queryFn: getCutoverPlans });
  const pack = useQuery({
    queryKey: ['go-no-go', selected],
    queryFn: () => getGoNoGoPack(selected!),
    enabled: selected !== null,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['cutover-plans'] });
    void queryClient.invalidateQueries({ queryKey: ['go-no-go'] });
  };

  const create = useMutation({
    mutationFn: () =>
      createCutoverPlan({
        code: planDraft.code,
        name: planDraft.name,
        cutoverDate: planDraft.cutoverDate,
        owner: planDraft.owner,
      }),
    onSuccess: (created) => {
      setSelected(created?.code ?? null);
      invalidate();
    },
  });
  const saveOperations = useMutation({
    mutationFn: () =>
      setOperationalPlan(selected!, {
        freezeFrom: operations.freezeFrom || null,
        freezeTo: operations.freezeTo || null,
        fallbackPlan: operations.fallbackPlan || null,
        supportModel: operations.supportModel || null,
      }),
    onSuccess: invalidate,
  });
  const add = useMutation({ mutationFn: () => addCriterion(selected!, criterion), onSuccess: invalidate });
  const record = useMutation({
    mutationFn: () =>
      recordCriterion(selected!, status.reference, {
        status: status.status,
        evidenceReference: status.evidenceReference || null,
        rationale: status.rationale || null,
      }),
    onSuccess: invalidate,
  });
  const approve = useMutation({ mutationFn: () => approveCutover(selected!, approval), onSuccess: invalidate });

  return (
    <section>
      <h1>Cutover</h1>
      <p className="muted">
        Entry and exit criteria with owners and evidence, the freeze window and fallback, and a go/no-go pack
        that shows the residual exposure a signature is actually accepting.
      </p>

      {plans.isError && <p className="error">{plans.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Cutover date</th>
            <th>Owner</th>
            <th>Freeze</th>
            <th>Criteria</th>
            <th>Approvals</th>
          </tr>
        </thead>
        <tbody>
          {plans.data?.map((plan) => (
            <tr key={plan.id}>
              <td>
                <button className="link" onClick={() => setSelected(plan.code)} type="button">
                  {plan.code}
                </button>
              </td>
              <td>{plan.name}</td>
              <td>{plan.cutoverDate}</td>
              <td>{plan.owner}</td>
              <td>{plan.isFrozen ? `${plan.freezeFrom} → ${plan.freezeTo}` : 'open'}</td>
              <td>{plan.criteria.length}</td>
              <td>{plan.approvals.length}</td>
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
              Cutover date
              <input
                onChange={(event) => setPlanDraft({ ...planDraft, cutoverDate: event.target.value })}
                type="date"
                value={planDraft.cutoverDate}
              />
            </label>
            <label>
              Owner
              <input onChange={(event) => setPlanDraft({ ...planDraft, owner: event.target.value })} value={planDraft.owner} />
            </label>
            <button
              disabled={!planDraft.code || !planDraft.name || !planDraft.cutoverDate || !planDraft.owner || create.isPending}
              onClick={() => create.mutate()}
              type="button"
            >
              Create
            </button>
          </div>
          {create.isError && <p className="error">{create.error.message}</p>}
        </div>
      )}

      {pack.isError && <p className="error">{pack.error.message}</p>}
      {pack.data && (
        <>
          <h2>
            Go/no-go — {pack.data.plan.code}: <strong>{pack.data.recommendation}</strong>
          </h2>
          <div className="metrics">
            <Metric label="Residual exposure" value={pack.data.residualExposure.toLocaleString()} tone="risk" />
            <Metric label="Tolerance" value={pack.data.residualExposureTolerance.toLocaleString()} />
            <Metric label="Payments at risk" value={pack.data.paymentsAtRisk.toLocaleString()} tone="risk" />
            <Metric label="Open cases" value={pack.data.openCases} />
            <Metric label="Expired exceptions" value={pack.data.expiredExceptions} tone="risk" />
            <Metric label="Open defects" value={pack.data.openDefects} tone="risk" />
            <Metric label="Test coverage" value={`${pack.data.testCoveragePercent.toFixed(1)}%`} />
            <Metric label="UAT mismatches" value={pack.data.uatMismatches} tone="risk" />
            <Metric label="Entry outstanding" value={pack.data.entryCriteriaOutstanding} />
            <Metric label="Exit outstanding" value={pack.data.exitCriteriaOutstanding} />
            <Metric label="Waived" value={pack.data.waivedCriteria} />
          </div>
          <p className="muted">
            Based on run {pack.data.basedOnRunId ?? 'none'}
            {pack.data.basedOnRunAtUtc && ` captured ${new Date(pack.data.basedOnRunAtUtc).toLocaleString()}`}, generated{' '}
            {new Date(pack.data.generatedAtUtc).toLocaleString()}.
          </p>

          <table className="table">
            <thead>
              <tr>
                <th>Reference</th>
                <th>Kind</th>
                <th>Description</th>
                <th>Owner</th>
                <th>Blocking</th>
                <th>Status</th>
                <th>Evidence</th>
                <th>Rationale</th>
              </tr>
            </thead>
            <tbody>
              {pack.data.plan.criteria.map((item) => (
                <tr key={item.id}>
                  <td>{item.reference}</td>
                  <td>{item.kind}</td>
                  <td>{item.description}</td>
                  <td>{item.owner}</td>
                  <td>{item.isBlocking ? 'yes' : 'no'}</td>
                  <td className={item.status === 'Failed' ? 'error' : undefined}>{item.status}</td>
                  <td>{item.evidenceReference ?? '—'}</td>
                  <td>{item.rationale ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <h3>Approvals</h3>
          <table className="table">
            <thead>
              <tr>
                <th>Role</th>
                <th>Approver</th>
                <th>Decision</th>
                <th>Recommendation at sign-off</th>
                <th>Rationale</th>
                <th>Decided</th>
              </tr>
            </thead>
            <tbody>
              {pack.data.plan.approvals.map((item) => (
                <tr key={item.id}>
                  <td>{item.role}</td>
                  <td>{item.approver}</td>
                  <td className={item.decision === 'Rejected' ? 'error' : undefined}>{item.decision}</td>
                  <td>{item.recommendationAtSignOff}</td>
                  <td>{item.rationale}</td>
                  <td>{new Date(item.decidedAtUtc).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {canWrite && (
            <>
              <div className="card">
                <h3>Operational plan</h3>
                <div className="filters">
                  <label>
                    Freeze from
                    <input
                      onChange={(event) => setOperations({ ...operations, freezeFrom: event.target.value })}
                      type="date"
                      value={operations.freezeFrom}
                    />
                  </label>
                  <label>
                    Freeze to
                    <input
                      onChange={(event) => setOperations({ ...operations, freezeTo: event.target.value })}
                      type="date"
                      value={operations.freezeTo}
                    />
                  </label>
                  <label>
                    Fallback
                    <input
                      onChange={(event) => setOperations({ ...operations, fallbackPlan: event.target.value })}
                      value={operations.fallbackPlan}
                    />
                  </label>
                  <label>
                    Support model
                    <input
                      onChange={(event) => setOperations({ ...operations, supportModel: event.target.value })}
                      value={operations.supportModel}
                    />
                  </label>
                  <button disabled={saveOperations.isPending} onClick={() => saveOperations.mutate()} type="button">
                    Save
                  </button>
                </div>
                {saveOperations.isError && <p className="error">{saveOperations.error.message}</p>}
              </div>

              <div className="card">
                <h3>Add criterion</h3>
                <div className="filters">
                  <label>
                    Reference
                    <input
                      onChange={(event) => setCriterion({ ...criterion, reference: event.target.value.toUpperCase() })}
                      value={criterion.reference}
                    />
                  </label>
                  <label>
                    Kind
                    <select
                      onChange={(event) => setCriterion({ ...criterion, kind: event.target.value as CriterionKind })}
                      value={criterion.kind}
                    >
                      <option value="Entry">Entry</option>
                      <option value="Exit">Exit</option>
                    </select>
                  </label>
                  <label>
                    Description
                    <input
                      onChange={(event) => setCriterion({ ...criterion, description: event.target.value })}
                      value={criterion.description}
                    />
                  </label>
                  <label>
                    Owner
                    <input onChange={(event) => setCriterion({ ...criterion, owner: event.target.value })} value={criterion.owner} />
                  </label>
                  <label>
                    Blocking
                    <input
                      checked={criterion.isBlocking}
                      onChange={(event) => setCriterion({ ...criterion, isBlocking: event.target.checked })}
                      type="checkbox"
                    />
                  </label>
                  <button
                    disabled={!criterion.reference || !criterion.description || !criterion.owner || add.isPending}
                    onClick={() => add.mutate()}
                    type="button"
                  >
                    Add
                  </button>
                </div>
                {add.isError && <p className="error">{add.error.message}</p>}
              </div>

              <div className="card">
                <h3>Record criterion status</h3>
                <div className="filters">
                  <label>
                    Reference
                    <input
                      onChange={(event) => setStatus({ ...status, reference: event.target.value.toUpperCase() })}
                      value={status.reference}
                    />
                  </label>
                  <label>
                    Status
                    <select
                      onChange={(event) => setStatus({ ...status, status: event.target.value as CriterionStatus })}
                      value={status.status}
                    >
                      {criterionStatuses.map((value) => (
                        <option key={value} value={value}>
                          {value}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Evidence
                    <input
                      onChange={(event) => setStatus({ ...status, evidenceReference: event.target.value })}
                      value={status.evidenceReference}
                    />
                  </label>
                  <label>
                    Rationale
                    <input onChange={(event) => setStatus({ ...status, rationale: event.target.value })} value={status.rationale} />
                  </label>
                  <button disabled={!status.reference || record.isPending} onClick={() => record.mutate()} type="button">
                    Record
                  </button>
                </div>
                {record.isError && <p className="error">{record.error.message}</p>}
              </div>
            </>
          )}

          {canApprove && (
            <div className="card">
              <h3>Sign off</h3>
              <div className="filters">
                <label>
                  Role
                  <input onChange={(event) => setApproval({ ...approval, role: event.target.value })} value={approval.role} />
                </label>
                <label>
                  Decision
                  <select
                    onChange={(event) => setApproval({ ...approval, decision: event.target.value as ApprovalDecision })}
                    value={approval.decision}
                  >
                    <option value="Approved">Approved</option>
                    <option value="Rejected">Rejected</option>
                  </select>
                </label>
                <label>
                  Rationale
                  <input onChange={(event) => setApproval({ ...approval, rationale: event.target.value })} value={approval.rationale} />
                </label>
                <button
                  disabled={!approval.role || !approval.rationale || approve.isPending}
                  onClick={() => approve.mutate()}
                  type="button"
                >
                  Sign
                </button>
              </div>
              {approve.isError && <p className="error">{approve.error.message}</p>}
            </div>
          )}
        </>
      )}
    </section>
  );
}
