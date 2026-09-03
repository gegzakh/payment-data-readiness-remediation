import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { getSettings, updateSetting } from '../api/releases';
import { hasPermission } from '../auth/keycloak';

export function SettingsPage() {
  const queryClient = useQueryClient();
  const settings = useQuery({ queryKey: ['settings'], queryFn: getSettings });
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const canWrite = hasPermission('settings.write');

  const save = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) => updateSetting(key, value),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings'] }),
  });

  return (
    <section>
      <h1>Runtime settings</h1>
      <p className="muted">Changes apply immediately; no redeploy is required.</p>
      <table className="table">
        <thead>
          <tr>
            <th>Key</th>
            <th>Value</th>
            <th>Description</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {settings.data?.map((setting) => (
            <tr key={setting.key}>
              <td>{setting.key}</td>
              <td>
                <input
                  disabled={!canWrite}
                  onChange={(event) => setDrafts({ ...drafts, [setting.key]: event.target.value })}
                  value={drafts[setting.key] ?? setting.value}
                />
              </td>
              <td className="muted">{setting.description}</td>
              <td>
                {canWrite && (
                  <button
                    disabled={save.isPending}
                    onClick={() => save.mutate({ key: setting.key, value: drafts[setting.key] ?? setting.value })}
                    type="button"
                  >
                    Save
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {save.isError && <p className="error">{save.error.message}</p>}
    </section>
  );
}
