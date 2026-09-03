import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createRelease, getReleases, publishRelease, type CreateReleaseInput } from '../api/releases';
import { hasPermission } from '../auth/keycloak';
import { ReleaseForm } from '../components/ReleaseForm';

export function ReleasesAdminPage() {
  const queryClient = useQueryClient();
  const canWrite = hasPermission('releasenotes.write');
  const canPublish = hasPermission('releasenotes.publish');
  const releases = useQuery({
    queryKey: ['admin-releases', canWrite],
    queryFn: () => getReleases(1, canWrite),
  });

  const create = useMutation({
    mutationFn: (input: CreateReleaseInput) => createRelease(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-releases'] }),
  });

  const publish = useMutation({
    mutationFn: (id: string) => publishRelease(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin-releases'] }),
  });

  return (
    <section>
      <h1>Releases</h1>

      {canWrite && <ReleaseForm onSubmit={(input) => create.mutate(input)} pending={create.isPending} />}
      {create.isError && <p className="error">{create.error.message}</p>}

      {releases.isPending && <p>Loading…</p>}
      {releases.isError && <p className="error">Releases could not be loaded: {releases.error.message}</p>}
      <table className="table">
        <thead>
          <tr>
            <th>Version</th>
            <th>Title</th>
            <th>Date</th>
            <th>Status</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {releases.data?.items.map((release) => (
            <tr key={release.id}>
              <td>{release.version}</td>
              <td>{release.title}</td>
              <td>{release.releaseDate}</td>
              <td>{release.status}</td>
              <td>
                {canPublish && release.status === 'Draft' && (
                  <button disabled={publish.isPending} onClick={() => publish.mutate(release.id)} type="button">
                    Publish
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {releases.data?.items.length === 0 && <p className="muted">No releases yet.</p>}
      {publish.isError && <p className="error">{publish.error.message}</p>}
    </section>
  );
}
