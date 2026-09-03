import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getAllowedPageSizes, getReleases, type ReleaseDto } from '../api/releases';
import { Pagination } from '../components/Pagination';
import { ReleaseCard } from '../components/ReleaseCard';

export function ReleaseNotesPage() {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState<number | undefined>(undefined);
  const [component, setComponent] = useState('');

  const pageSizes = useQuery({ queryKey: ['page-sizes'], queryFn: getAllowedPageSizes });
  const releases = useQuery({
    queryKey: ['releases', page, pageSize, component],
    queryFn: () => getReleases({ page, pageSize, component: component || undefined }),
  });

  return (
    <section>
      <h1>Release notes</h1>
      <p className="muted">Everything shipped to the platform, newest release first.</p>

      <div className="toolbar">
        <label>
          Component
          <input
            onChange={(event) => {
              setComponent(event.target.value);
              setPage(1);
            }}
            placeholder="All components"
            value={component}
          />
        </label>
        <label>
          Per page
          <select
            onChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(1);
            }}
            value={pageSize ?? releases.data?.pageSize ?? ''}
          >
            {(pageSizes.data ?? []).map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
      </div>

      {releases.isPending && <p>Loading release notes…</p>}
      {releases.isError && <p className="error">Release notes are unavailable right now.</p>}

      {releases.data?.items.map((release: ReleaseDto) => <ReleaseCard key={release.id} release={release} />)}
      {releases.data?.items.length === 0 && <p>No releases match this filter yet.</p>}

      {releases.data && (
        <Pagination
          onPageChange={setPage}
          page={releases.data.page}
          totalCount={releases.data.totalCount}
          totalPages={releases.data.totalPages}
        />
      )}
    </section>
  );
}
