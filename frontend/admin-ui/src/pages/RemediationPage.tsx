import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  addEvidence,
  applyBulk,
  casePriorities,
  caseStatuses,
  decideCase,
  generateCases,
  getCase,
  getCases,
  getFunnel,
  previewBulk,
  proposeCorrection,
  submitCase,
  type BulkPreviewDto,
  type BulkSelection,
  type CaseFilter,
  type CasePriority,
  type CaseStatus,
  type DecisionType,
} from '../api/remediation';
import { hasPermission } from '../auth/keycloak';
import { Metric } from '../components/Metric';

const decisions: DecisionType[] = ['Approve', 'Return', 'Reject', 'Dismiss', 'GrantException'];

const emptyProposal = {
  country: '',
  townName: '',
  postCode: '',
  streetName: '',
  buildingNumber: '',
  notes: '',
};

export function RemediationPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('remediation.write');
  const canApprove = hasPermission('remediation.approve');

  const [page, setPage] = useState(1);
  const [filter, setFilter] = useState<CaseFilter>({});
  const [selectedCase, setSelectedCase] = useState<string | null>(null);
  const [proposal, setProposal] = useState(emptyProposal);
  const [evidence, setEvidence] = useState({ kind: 'CustomerConfirmation', reference: '', description: '' });
  const [decision, setDecision] = useState<DecisionType>('Approve');
  const [rationale, setRationale] = useState('');
  const [expiresOn, setExpiresOn] = useState('');
  const [bulkPreview, setBulkPreview] = useState<BulkPreviewDto | null>(null);

  const funnel = useQuery({ queryKey: ['remediation-funnel'], queryFn: getFunnel });
  const cases = useQuery({ queryKey: ['remediation-cases', page, filter], queryFn: () => getCases(page, filter) });
  const detail = useQuery({
    queryKey: ['remediation-case', selectedCase],
    queryFn: () => getCase(selectedCase!),
    enabled: selectedCase !== null,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['remediation-cases'] });
    void queryClient.invalidateQueries({ queryKey: ['remediation-case'] });
    void queryClient.invalidateQueries({ queryKey: ['remediation-funnel'] });
  };

  const generate = useMutation({ mutationFn: () => generateCases(), onSuccess: invalidate });
  const propose = useMutation({
    mutationFn: () => proposeCorrection(selectedCase!, proposal),
    onSuccess: invalidate,
  });
  const attach = useMutation({ mutationFn: () => addEvidence(selectedCase!, evidence), onSuccess: invalidate });
  const submit = useMutation({ mutationFn: () => submitCase(selectedCase!), onSuccess: invalidate });
  const decide = useMutation({
    mutationFn: () => decideCase(selectedCase!, decision, rationale, expiresOn),
    onSuccess: invalidate,
  });

  const selection: BulkSelection = {
    sourceCode: filter.sourceCode || undefined,
    queue: filter.queue || undefined,
    ruleCode: filter.ruleCode || undefined,
    status: filter.status || undefined,
    minimumPriority: filter.priority || undefined,
  };

  const preview = useMutation({
    mutationFn: () => previewBulk('approve', selection),
    onSuccess: (result) => setBulkPreview(result ?? null),
  });
  const applyAll = useMutation({
    mutationFn: () => applyBulk('approve', selection, rationale || 'Bulk approval of deterministic corrections'),
    onSuccess: () => {
      setBulkPreview(null);
      invalidate();
    },
  });

  const openCase = (caseId: string) => {
    setSelectedCase(selectedCase === caseId ? null : caseId);
    setProposal(emptyProposal);
  };

  const updateFilter = (patch: Partial<CaseFilter>) => {
    setPage(1);
    setFilter({ ...filter, ...patch });
  };

  return (
    <section>
      <h1>Remediation queue</h1>
      <p className="muted">
        One case per defective party address, however many payments it appeared in. Corrections are made by a
        maker and approved by somebody else before anything is written back to the source.
      </p>

      {funnel.isError && <p className="error">The funnel could not be loaded: {funnel.error.message}</p>}
      {funnel.data && (
        <div className="metrics">
          <Metric label="Open cases" value={funnel.data.openCases.toLocaleString()} />
          <Metric label="Awaiting approval" value={funnel.data.pendingApproval.toLocaleString()} />
          <Metric label="Approved" value={funnel.data.approved.toLocaleString()} />
          <Metric label="Remediated" value={`${funnel.data.remediationPercent.toFixed(1)}%`} />
          <Metric label="Exposure still open" value={funnel.data.futureExposureOpen.toLocaleString()} tone="risk" />
          <Metric label="Expired exceptions" value={funnel.data.expiredExceptions.toLocaleString()} tone="risk" />
          <Metric label="Overdue" value={funnel.data.overdue.toLocaleString()} tone="risk" />
        </div>
      )}

      {canWrite && (
        <p>
          <button disabled={generate.isPending} onClick={() => generate.mutate()} type="button">
            {generate.isPending ? 'Generating…' : 'Generate cases from the latest validation runs'}
          </button>
          {generate.isError && <span className="error"> {generate.error.message}</span>}
          {generate.data && (
            <span className="muted">
              {' '}
              {generate.data.casesCreated} opened, {generate.data.casesUpdated} updated,{' '}
              {generate.data.occurrencesFolded} occurrences folded.
            </span>
          )}
        </p>
      )}

      <div className="filters">
        <label>
          Status{' '}
          <select
            onChange={(event) => updateFilter({ status: event.target.value as CaseStatus | '' })}
            value={filter.status ?? ''}
          >
            <option value="">Any</option>
            {caseStatuses.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
        <label>
          Minimum priority{' '}
          <select
            onChange={(event) => updateFilter({ priority: event.target.value as CasePriority | '' })}
            value={filter.priority ?? ''}
          >
            <option value="">Any</option>
            {casePriorities.map((priority) => (
              <option key={priority} value={priority}>
                {priority}
              </option>
            ))}
          </select>
        </label>
        <label>
          Source <input onChange={(event) => updateFilter({ sourceCode: event.target.value })} value={filter.sourceCode ?? ''} />
        </label>
        <label>
          Rule <input onChange={(event) => updateFilter({ ruleCode: event.target.value })} value={filter.ruleCode ?? ''} />
        </label>
        <label>
          Overdue only{' '}
          <input
            checked={filter.overdueOnly ?? false}
            onChange={(event) => updateFilter({ overdueOnly: event.target.checked })}
            type="checkbox"
          />
        </label>
      </div>

      {canApprove && (
        <div className="card">
          <h2>Bulk approval</h2>
          <p className="muted">
            Applies to everything the filters above match. The preview shows why cases are held back — a low
            confidence proposal or a case you submitted yourself can never be approved in bulk.
          </p>
          <button disabled={preview.isPending} onClick={() => preview.mutate()} type="button">
            Preview
          </button>{' '}
          <button
            disabled={!bulkPreview || bulkPreview.eligibleCases === 0 || applyAll.isPending}
            onClick={() => applyAll.mutate()}
            type="button"
          >
            Approve {bulkPreview?.eligibleCases ?? 0} cases
          </button>
          {preview.isError && <p className="error">{preview.error.message}</p>}
          {applyAll.isError && <p className="error">{applyAll.error.message}</p>}
          {bulkPreview && (
            <>
              <p>
                {bulkPreview.matchedCases} matched, {bulkPreview.eligibleCases} eligible,{' '}
                {bulkPreview.blockedCases} blocked, {bulkPreview.futureExposure} payments of exposure.{' '}
                {bulkPreview.rollbackSupported ? 'Reversible.' : 'Not reversible.'}
              </p>
              {bulkPreview.blockedReasons.length > 0 && (
                <ul>
                  {bulkPreview.blockedReasons.map((reason) => (
                    <li key={reason}>{reason}</li>
                  ))}
                </ul>
              )}
            </>
          )}
        </div>
      )}

      {cases.isError && <p className="error">{cases.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Party</th>
            <th>Source</th>
            <th>Role</th>
            <th>Rules</th>
            <th>Payments</th>
            <th>At risk</th>
            <th>Priority</th>
            <th>Confidence</th>
            <th>Status</th>
            <th>Queue</th>
            <th>Due</th>
          </tr>
        </thead>
        <tbody>
          {cases.data?.items.map((item) => (
            <tr key={item.id}>
              <td>
                <button className="link" onClick={() => openCase(item.id)} type="button">
                  {item.partyName ?? item.caseKey}
                </button>
              </td>
              <td>{item.sourceCode}</td>
              <td>{item.partyRole}</td>
              <td>{item.issueRuleCodes}</td>
              <td>{item.occurrences}</td>
              <td className={item.futureExposure > 0 ? 'error' : undefined}>{item.futureExposure}</td>
              <td>{item.priority}</td>
              <td>{item.confidence === null || item.confidence === undefined ? '—' : `${item.confidence.toFixed(0)}%`}</td>
              <td>{item.status}</td>
              <td>{item.queue ?? '—'}</td>
              <td className={item.isOverdue ? 'error' : undefined}>{item.dueDate ?? '—'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      {cases.data && (
        <p className="pagination">
          <button disabled={page <= 1} onClick={() => setPage(page - 1)} type="button">
            Previous
          </button>
          Page {cases.data.page} of {Math.max(cases.data.totalPages, 1)}
          <button disabled={page >= cases.data.totalPages} onClick={() => setPage(page + 1)} type="button">
            Next
          </button>
        </p>
      )}

      {detail.data && (
        <div className="card">
          <h2>{detail.data.partyName ?? detail.data.caseKey}</h2>
          <p className="muted">
            {detail.data.sourceCode} · owner {detail.data.ownerName ?? 'unassigned'} · {detail.data.occurrences}{' '}
            payments · evidence pointer {detail.data.evidencePointer}
          </p>

          <table className="table">
            <thead>
              <tr>
                <th>Field</th>
                <th>In the source today</th>
                <th>Proposed</th>
                <th>Confidence</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>Country</td>
                <td>{detail.data.original.country ?? '—'}</td>
                <td>{detail.data.proposal?.country ?? '—'}</td>
                <td>{detail.data.proposal ? `${detail.data.proposal.countryConfidence.toFixed(0)}%` : '—'}</td>
              </tr>
              <tr>
                <td>Town</td>
                <td>{detail.data.original.townName ?? '—'}</td>
                <td>{detail.data.proposal?.townName ?? '—'}</td>
                <td>{detail.data.proposal ? `${detail.data.proposal.townConfidence.toFixed(0)}%` : '—'}</td>
              </tr>
              <tr>
                <td>Post code</td>
                <td>{detail.data.original.postCode ?? '—'}</td>
                <td>{detail.data.proposal?.postCode ?? '—'}</td>
                <td>{detail.data.proposal ? `${detail.data.proposal.postCodeConfidence.toFixed(0)}%` : '—'}</td>
              </tr>
              <tr>
                <td>Street</td>
                <td>{detail.data.original.streetName ?? '—'}</td>
                <td>{detail.data.proposal?.streetName ?? '—'}</td>
                <td>{detail.data.proposal ? `${detail.data.proposal.streetConfidence.toFixed(0)}%` : '—'}</td>
              </tr>
              <tr>
                <td>Building</td>
                <td>{detail.data.original.buildingNumber ?? '—'}</td>
                <td>{detail.data.proposal?.buildingNumber ?? '—'}</td>
                <td>
                  {detail.data.proposal ? `${detail.data.proposal.buildingNumberConfidence.toFixed(0)}%` : '—'}
                </td>
              </tr>
              <tr>
                <td>Unstructured lines</td>
                <td colSpan={3}>{detail.data.original.addressLines ?? '—'}</td>
              </tr>
            </tbody>
          </table>

          {detail.data.proposal && (
            <p className="muted">
              {detail.data.proposal.method} · overall {detail.data.proposal.overallConfidence.toFixed(0)}%
              {detail.data.proposal.requiresHumanVerification && ' · needs human verification'}
              {detail.data.proposal.ambiguity && ` · ambiguity: ${detail.data.proposal.ambiguity}`}
              {detail.data.proposal.alternatives && ` · alternatives: ${detail.data.proposal.alternatives}`}
            </p>
          )}

          {canWrite && (
            <div className="filters">
              <label>
                Country{' '}
                <input onChange={(event) => setProposal({ ...proposal, country: event.target.value })} value={proposal.country} />
              </label>
              <label>
                Town{' '}
                <input onChange={(event) => setProposal({ ...proposal, townName: event.target.value })} value={proposal.townName} />
              </label>
              <label>
                Post code{' '}
                <input onChange={(event) => setProposal({ ...proposal, postCode: event.target.value })} value={proposal.postCode} />
              </label>
              <label>
                Street{' '}
                <input onChange={(event) => setProposal({ ...proposal, streetName: event.target.value })} value={proposal.streetName} />
              </label>
              <label>
                Building{' '}
                <input
                  onChange={(event) => setProposal({ ...proposal, buildingNumber: event.target.value })}
                  value={proposal.buildingNumber}
                />
              </label>
              <label>
                Notes{' '}
                <input onChange={(event) => setProposal({ ...proposal, notes: event.target.value })} value={proposal.notes} />
              </label>
              <button disabled={propose.isPending} onClick={() => propose.mutate()} type="button">
                Save correction
              </button>
            </div>
          )}
          {propose.isError && <p className="error">{propose.error.message}</p>}

          <h3>Evidence</h3>
          <ul>
            {detail.data.evidence.length === 0 && <li className="muted">Nothing attached yet.</li>}
            {detail.data.evidence.map((item) => (
              <li key={item.id}>
                <strong>{item.kind}</strong> {item.reference} — {item.description ?? 'no description'} (
                {item.capturedBy})
              </li>
            ))}
          </ul>
          {canWrite && (
            <div className="filters">
              <label>
                Kind <input onChange={(event) => setEvidence({ ...evidence, kind: event.target.value })} value={evidence.kind} />
              </label>
              <label>
                Reference{' '}
                <input onChange={(event) => setEvidence({ ...evidence, reference: event.target.value })} value={evidence.reference} />
              </label>
              <label>
                Description{' '}
                <input
                  onChange={(event) => setEvidence({ ...evidence, description: event.target.value })}
                  value={evidence.description}
                />
              </label>
              <button disabled={!evidence.reference || attach.isPending} onClick={() => attach.mutate()} type="button">
                Attach
              </button>
              <button disabled={submit.isPending} onClick={() => submit.mutate()} type="button">
                Submit for approval
              </button>
            </div>
          )}
          {attach.isError && <p className="error">{attach.error.message}</p>}
          {submit.isError && <p className="error">{submit.error.message}</p>}

          {canApprove && (
            <>
              <h3>Decision</h3>
              <p className="muted">
                A correction cannot be approved by the person who submitted it. An exception is time-bound and
                still counts as exposure until the address is fixed.
              </p>
              <div className="filters">
                <label>
                  Decision{' '}
                  <select onChange={(event) => setDecision(event.target.value as DecisionType)} value={decision}>
                    {decisions.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Rationale <input onChange={(event) => setRationale(event.target.value)} value={rationale} />
                </label>
                {decision === 'GrantException' && (
                  <label>
                    Expires on{' '}
                    <input onChange={(event) => setExpiresOn(event.target.value)} type="date" value={expiresOn} />
                  </label>
                )}
                <button disabled={decide.isPending} onClick={() => decide.mutate()} type="button">
                  Record decision
                </button>
              </div>
              {decide.isError && <p className="error">{decide.error.message}</p>}
            </>
          )}

          <h3>History</h3>
          <table className="table">
            <thead>
              <tr>
                <th>When</th>
                <th>Action</th>
                <th>From</th>
                <th>To</th>
                <th>Actor</th>
                <th>Rationale</th>
              </tr>
            </thead>
            <tbody>
              {detail.data.history.map((entry) => (
                <tr key={entry.id}>
                  <td>{entry.occurredAtUtc.replace('T', ' ').slice(0, 19)}</td>
                  <td>{entry.action}</td>
                  <td>{entry.fromStatus}</td>
                  <td>{entry.toStatus}</td>
                  <td>{entry.actor}</td>
                  <td>{entry.rationale ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
