using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.Audit.Application.Abstractions;
using PDR.Audit.Domain.Ledger;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;

namespace PDR.Audit.Application.Ledger.Queries;

/// <summary>Searchable audit trail, newest first (FR-AUD-003).</summary>
public sealed record GetAuditRecordsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Service = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    string? Actor = null,
    string? CorrelationId = null,
    AuditOutcome? Outcome = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IQuery<PagedResult<AuditRecordDto>>;

public sealed record GetAuditRecordByIdQuery(Guid Id) : IQuery<AuditRecordDto>;

public sealed record VerifyAuditChainQuery(long? FromSequence = null) : IQuery<AuditChainVerificationDto>;

public sealed class GetAuditRecordsQueryValidator : AbstractValidator<GetAuditRecordsQuery>
{
    public GetAuditRecordsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 200);
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From!.Value)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}

public sealed class GetAuditRecordsQueryHandler(IAuditDbContext context, IClock clock)
    : IRequestHandler<GetAuditRecordsQuery, Result<PagedResult<AuditRecordDto>>>
{
    public async Task<Result<PagedResult<AuditRecordDto>>> HandleAsync(
        GetAuditRecordsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.AuditRecords.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Service))
        {
            query = query.Where(record => record.Service == request.Service);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(record => record.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(record => record.EntityType == request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            query = query.Where(record => record.EntityId == request.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            query = query.Where(record => record.Actor == request.Actor);
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            query = query.Where(record => record.CorrelationId == request.CorrelationId);
        }

        if (request.Outcome is not null)
        {
            query = query.Where(record => record.Outcome == request.Outcome);
        }

        if (request.From is not null)
        {
            query = query.Where(record => record.OccurredAtUtc >= request.From);
        }

        if (request.To is not null)
        {
            query = query.Where(record => record.OccurredAtUtc <= request.To);
        }

        var total = await query.LongCountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(record => record.Sequence)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<AuditRecordDto>(
            records.Select(AuditMapping.ToDto).ToList(),
            request.Page,
            request.PageSize,
            total,
            clock.UtcNow));
    }
}

public sealed class GetAuditRecordByIdQueryHandler(IAuditDbContext context)
    : IRequestHandler<GetAuditRecordByIdQuery, Result<AuditRecordDto>>
{
    public async Task<Result<AuditRecordDto>> HandleAsync(
        GetAuditRecordByIdQuery request,
        CancellationToken cancellationToken)
    {
        var record = await context.AuditRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.Id, cancellationToken);

        return record is null
            ? Result.Failure<AuditRecordDto>(AuditErrors.NotFound(request.Id))
            : record.ToDto();
    }
}

/// <summary>
/// Re-hashes the ledger in sequence order and reports the first record that no longer reproduces its
/// stored hash — the tamper-evidence check auditors run (FR-AUD-002).
/// </summary>
public sealed class VerifyAuditChainQueryHandler(IAuditDbContext context, IClock clock)
    : IRequestHandler<VerifyAuditChainQuery, Result<AuditChainVerificationDto>>
{
    private const int BatchSize = 500;

    public async Task<Result<AuditChainVerificationDto>> HandleAsync(
        VerifyAuditChainQuery request,
        CancellationToken cancellationToken)
    {
        AuditRecord? previous = null;
        var checkedRecords = 0L;
        var cursor = 0L;

        if (request.FromSequence is > 1)
        {
            previous = await context.AuditRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(record => record.Sequence == request.FromSequence - 1, cancellationToken);
            cursor = request.FromSequence.Value - 1;
        }

        while (true)
        {
            var batch = await context.AuditRecords
                .AsNoTracking()
                .Where(record => record.Sequence > cursor)
                .OrderBy(record => record.Sequence)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var record in batch)
            {
                if (!record.IsIntact(previous))
                {
                    return new AuditChainVerificationDto(false, checkedRecords, record.Sequence, clock.UtcNow);
                }

                previous = record;
                checkedRecords++;
                cursor = record.Sequence;
            }
        }

        return new AuditChainVerificationDto(true, checkedRecords, null, clock.UtcNow);
    }
}
