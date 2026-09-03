using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Core.Paging;

namespace PDR.BuildingBlocks.Persistence.Paging;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.LongCountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount, asOfUtc);
    }
}
