import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import {
  activateVersion,
  addRule,
  addVersion,
  getRulesets,
  getSchemes,
  ruleApplicabilities,
  ruleKinds,
  ruleSeverities,
  type RuleInput,
  type RulesetDto,
  type RulesetVersionDto,
} from '../api/rules';
import { hasPermission } from '../auth/keycloak';

const emptyRule: RuleInput = {
  code: '',
  field: '',
  kind: 'Required',
  severity: 'Error',
  applicability: 'Both',
  message: '',
  parameter: '',
};

export function RulesAdminPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('rules.write');
  const canActivate = hasPermission('rules.activate');

  const schemes = useQuery({ queryKey: ['schemes'], queryFn: getSchemes });
  const rulesets = useQuery({ queryKey: ['rulesets'], queryFn: getRulesets });

  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [rule, setRule] = useState<RuleInput>(emptyRule);
  const [ruleVersion, setRuleVersion] = useState<number | null>(null);
  const [effectiveFrom, setEffectiveFrom] = useState(new Date().toISOString().slice(0, 10));

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['rulesets'] });

  const newVersion = useMutation({
    mutationFn: ({ id, copyFrom }: { id: string; copyFrom: number | null }) =>
      addVersion(id, copyFrom, 'Drafted in admin UI'),
    onSuccess: (versionNumber) => {
      setRuleVersion(versionNumber ?? null);
      return invalidate();
    },
  });

  const createRule = useMutation({
    mutationFn: ({ id, versionNumber, input }: { id: string; versionNumber: number; input: RuleInput }) =>
      addRule(id, versionNumber, { ...input, parameter: input.parameter?.trim() ? input.parameter : null }),
    onSuccess: () => {
      setRule(emptyRule);
      return invalidate();
    },
  });

  const activate = useMutation({
    mutationFn: ({ id, versionNumber }: { id: string; versionNumber: number }) =>
      activateVersion(id, versionNumber, effectiveFrom),
    onSuccess: invalidate,
  });

  const selected: RulesetDto | undefined =
    rulesets.data?.find((ruleset) => ruleset.id === selectedId) ?? rulesets.data?.[0];

  const draftVersions = selected?.versions.filter((version) => version.status === 'Draft') ?? [];

  // The selected draft disappears once it is activated, so fall back to one that still exists.
  const targetVersion =
    draftVersions.find((version) => version.versionNumber === ruleVersion)?.versionNumber ??
    draftVersions.at(-1)?.versionNumber ??
    null;

  const versionLabel = (version: RulesetVersionDto) =>
    `v${version.versionNumber} · ${version.status}${version.effectiveFrom ? ` from ${version.effectiveFrom}` : ''}`;

  return (
    <section>
      <h1>Scheme rules</h1>
      <p className="muted">
        Validation evaluates the version that was effective on a given date. Activating an earlier, retired version is
        the rollback path.
      </p>

      {schemes.isError && <p className="error">Schemes could not be loaded: {schemes.error.message}</p>}
      {rulesets.isError && <p className="error">Rulesets could not be loaded: {rulesets.error.message}</p>}
      {(schemes.isPending || rulesets.isPending) && <p>Loading…</p>}

      <h2>Schemes</h2>
      <table className="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Name</th>
            <th>Structured address mandatory from</th>
            <th>Active</th>
          </tr>
        </thead>
        <tbody>
          {schemes.data?.map((scheme) => (
            <tr key={scheme.id}>
              <td>{scheme.code}</td>
              <td>{scheme.name}</td>
              <td>{scheme.structuredAddressMandatoryFrom ?? '—'}</td>
              <td>{scheme.isActive ? 'Yes' : 'No'}</td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2>Rulesets</h2>
      <label>
        Ruleset{' '}
        <select onChange={(event) => setSelectedId(event.target.value)} value={selected?.id ?? ''}>
          {rulesets.data?.map((ruleset) => (
            <option key={ruleset.id} value={ruleset.id}>
              {ruleset.schemeCode} — {ruleset.name}
            </option>
          ))}
        </select>
      </label>

      {selected && (
        <>
          {canWrite && (
            <p>
              <button
                disabled={newVersion.isPending}
                onClick={() =>
                  newVersion.mutate({ id: selected.id, copyFrom: selected.activeVersionNumber ?? null })
                }
                type="button"
              >
                New draft version (copy of v{selected.activeVersionNumber ?? '—'})
              </button>
            </p>
          )}
          {newVersion.isError && <p className="error">{newVersion.error.message}</p>}

          {selected.versions.map((version) => (
            <article key={version.id}>
              <h3>{versionLabel(version)}</h3>
              {canActivate && version.status !== 'Active' && (
                <p>
                  <label>
                    Effective from{' '}
                    <input
                      onChange={(event) => setEffectiveFrom(event.target.value)}
                      type="date"
                      value={effectiveFrom}
                    />
                  </label>{' '}
                  <button
                    disabled={activate.isPending}
                    onClick={() => activate.mutate({ id: selected.id, versionNumber: version.versionNumber })}
                    type="button"
                  >
                    {version.status === 'Retired' ? 'Roll back to this version' : 'Activate'}
                  </button>
                </p>
              )}
              <table className="table">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Field</th>
                    <th>Kind</th>
                    <th>Parameter</th>
                    <th>Severity</th>
                    <th>Applies to</th>
                    <th>Message</th>
                  </tr>
                </thead>
                <tbody>
                  {version.rules.map((definition) => (
                    <tr key={definition.id}>
                      <td>{definition.code}</td>
                      <td>{definition.field}</td>
                      <td>{definition.kind}</td>
                      <td>{definition.parameter ?? '—'}</td>
                      <td>{definition.severity}</td>
                      <td>{definition.applicability}</td>
                      <td className="muted">{definition.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </article>
          ))}
          {activate.isError && <p className="error">{activate.error.message}</p>}

          {canWrite && (
            <form
              onSubmit={(event) => {
                event.preventDefault();
                if (targetVersion !== null) {
                  createRule.mutate({ id: selected.id, versionNumber: targetVersion, input: rule });
                }
              }}
            >
              <h3>Add a rule to a draft version</h3>
              <label>
                Version{' '}
                <select onChange={(event) => setRuleVersion(Number(event.target.value))} value={targetVersion ?? ''}>
                  {draftVersions.map((version) => (
                    <option key={version.id} value={version.versionNumber}>
                      v{version.versionNumber}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Code <input onChange={(event) => setRule({ ...rule, code: event.target.value })} value={rule.code} />
              </label>
              <label>
                Field <input onChange={(event) => setRule({ ...rule, field: event.target.value })} value={rule.field} />
              </label>
              <label>
                Kind{' '}
                <select
                  onChange={(event) => setRule({ ...rule, kind: event.target.value as RuleInput['kind'] })}
                  value={rule.kind}
                >
                  {ruleKinds.map((kind) => (
                    <option key={kind} value={kind}>
                      {kind}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Parameter{' '}
                <input
                  onChange={(event) => setRule({ ...rule, parameter: event.target.value })}
                  placeholder="length, regex or CSV"
                  value={rule.parameter ?? ''}
                />
              </label>
              <label>
                Severity{' '}
                <select
                  onChange={(event) => setRule({ ...rule, severity: event.target.value as RuleInput['severity'] })}
                  value={rule.severity}
                >
                  {ruleSeverities.map((severity) => (
                    <option key={severity} value={severity}>
                      {severity}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Applies to{' '}
                <select
                  onChange={(event) =>
                    setRule({ ...rule, applicability: event.target.value as RuleInput['applicability'] })
                  }
                  value={rule.applicability}
                >
                  {ruleApplicabilities.map((applicability) => (
                    <option key={applicability} value={applicability}>
                      {applicability}
                    </option>
                  ))}
                </select>
              </label>
              <label>
                Message{' '}
                <input onChange={(event) => setRule({ ...rule, message: event.target.value })} value={rule.message} />
              </label>
              <button disabled={createRule.isPending || targetVersion === null} type="submit">
                Add rule
              </button>
              {createRule.isError && <p className="error">{createRule.error.message}</p>}
            </form>
          )}
        </>
      )}
    </section>
  );
}
