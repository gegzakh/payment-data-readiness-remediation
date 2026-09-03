interface PaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  return (
    <nav aria-label="Release notes pages" className="pagination">
      <button disabled={page <= 1} onClick={() => onPageChange(page - 1)} type="button">
        Previous
      </button>
      <span>
        Page {page} of {Math.max(totalPages, 1)} ({totalCount} releases)
      </span>
      <button disabled={page >= totalPages} onClick={() => onPageChange(page + 1)} type="button">
        Next
      </button>
    </nav>
  );
}
