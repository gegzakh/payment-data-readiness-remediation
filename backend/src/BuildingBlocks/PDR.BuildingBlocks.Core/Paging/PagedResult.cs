namespace PDR.BuildingBlocks.Core.Paging;

/// <summary>
/// Standard envelope for every paged endpoint. <see cref="AsOfUtc"/> satisfies FR-REP-002
/// (every metric/list must declare the point in time it was produced for).
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount,
    DateTimeOffset AsOfUtc)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize, DateTimeOffset asOfUtc) =>
        new([], page, pageSize, 0, asOfUtc);
}
