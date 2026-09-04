using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Ingestion.Application.Abstractions;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest.Queries;

public sealed record GetBatchesQuery(
    int Page = 1,
    int? PageSize = null,
    BatchStatus? Status = null,
    string? SourceCode = null) : IQuery<PagedResult<IngestionBatchDto>>;

public sealed record GetBatchByIdQuery(Guid BatchId) : IQuery<IngestionBatchDto>;

public sealed record GetBatchRecordsQuery(
    Guid BatchId,
    int Page = 1,
    int? PageSize = null,
    bool DuplicatesOnly = false) : IQuery<PagedResult<PartyAddressRecordDto>>;

public sealed record GetIngestionOverviewQuery : IQuery<IngestionOverviewDto>;

/// <summary>
/// Every parsed record of a batch, unmasked, for the validation service. Exposed only on the internal
/// route that the gateway does not publish, and gated on the permission to run validation.
/// </summary>
public sealed record ExportBatchRecordsQuery(Guid BatchId) : IQuery<IReadOnlyList<PartyAddressRecordDto>>;

public sealed class GetBatchesQueryHandler(IIngestionDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetBatchesQuery, Result<PagedResult<IngestionBatchDto>>>
{
    public async Task<Result<PagedResult<IngestionBatchDto>>> HandleAsync(
        GetBatchesQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await IngestionPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);

        var query = context.Batches.AsNoTracking();

        if (request.Status is { } status)
        {
            query = query.Where(batch => batch.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var sourceCode = request.SourceCode.ToUpperInvariant();
            query = query.Where(batch => batch.SourceCode == sourceCode);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var batches = await query
            .OrderByDescending(batch => batch.ReceivedAtUtc)
            .ThenBy(batch => batch.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<IngestionBatchDto>(
            [.. batches.Select(batch => batch.ToDto())],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

public sealed class GetBatchByIdQueryHandler(IIngestionDbContext context)
    : IRequestHandler<GetBatchByIdQuery, Result<IngestionBatchDto>>
{
    public async Task<Result<IngestionBatchDto>> HandleAsync(
        GetBatchByIdQuery request,
        CancellationToken cancellationToken)
    {
        var batch = await context.Batches
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == request.BatchId, cancellationToken);

        return batch is null
            ? Result.Failure<IngestionBatchDto>(BatchErrors.NotFound(request.BatchId))
            : batch.ToDto();
    }
}

public sealed class GetBatchRecordsQueryHandler(
    IIngestionDbContext context,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetBatchRecordsQuery, Result<PagedResult<PartyAddressRecordDto>>>
{
    public async Task<Result<PagedResult<PartyAddressRecordDto>>> HandleAsync(
        GetBatchRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await context.Batches.AnyAsync(batch => batch.Id == request.BatchId, cancellationToken))
        {
            return Result.Failure<PagedResult<PartyAddressRecordDto>>(BatchErrors.NotFound(request.BatchId));
        }

        var pageSize = await IngestionPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);
        var unmasked = currentUser.HasPermission(Permissions.Validation.DrillDown);

        var query = context.Records
            .AsNoTracking()
            .Where(record => record.BatchId == request.BatchId);

        if (request.DuplicatesOnly)
        {
            query = query.Where(record => record.IsDuplicate);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var records = await query
            .OrderBy(record => record.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PartyAddressRecordDto>(
            [.. records.Select(record => record.ToDto(unmasked))],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

public sealed class ExportBatchRecordsQueryHandler(IIngestionDbContext context)
    : IRequestHandler<ExportBatchRecordsQuery, Result<IReadOnlyList<PartyAddressRecordDto>>>
{
    public async Task<Result<IReadOnlyList<PartyAddressRecordDto>>> HandleAsync(
        ExportBatchRecordsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await context.Batches.AnyAsync(batch => batch.Id == request.BatchId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<PartyAddressRecordDto>>(BatchErrors.NotFound(request.BatchId));
        }

        var records = await context.Records
            .AsNoTracking()
            .Where(record => record.BatchId == request.BatchId)
            .OrderBy(record => record.Sequence)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PartyAddressRecordDto>>(
            [.. records.Select(record => record.ToDto(unmasked: true))]);
    }
}

public sealed class GetIngestionOverviewQueryHandler(IIngestionDbContext context, IClock clock)
    : IRequestHandler<GetIngestionOverviewQuery, Result<IngestionOverviewDto>>
{
    public async Task<Result<IngestionOverviewDto>> HandleAsync(
        GetIngestionOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var batches = await context.Batches
            .AsNoTracking()
            .Select(batch => new { batch.Status, batch.ParsedCount, batch.DuplicateCount })
            .ToListAsync(cancellationToken);

        return new IngestionOverviewDto(
            batches.Count,
            batches.Count(batch => batch.Status == BatchStatus.Parsed),
            batches.Count(batch => batch.Status == BatchStatus.Quarantined),
            batches.Count(batch => batch.Status == BatchStatus.Failed),
            batches.Sum(batch => batch.ParsedCount),
            batches.Sum(batch => batch.DuplicateCount),
            clock.UtcNow);
    }
}

internal static class IngestionPageSize
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 200;

    public static async Task<int> ResolveAsync(ISettingsReader settings, int? requested, CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(IngestionSettingKeys.PageSize, DefaultPageSize, cancellationToken);
        var pageSize = requested ?? configured;
        return Math.Clamp(pageSize, 1, MaxPageSize);
    }
}
