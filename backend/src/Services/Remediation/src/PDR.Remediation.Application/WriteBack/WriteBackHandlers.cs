using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.Remediation.Application.Abstractions;
using PDR.Remediation.Application.Cases;
using PDR.Remediation.Domain.WriteBack;

namespace PDR.Remediation.Application.WriteBack;

public sealed record PreviewWriteBackCommand(string SourceCode, IReadOnlyList<Guid>? CaseIds)
    : ICommand<WriteBackPreviewDto>;

public sealed record ApplyWriteBackCommand(string SourceCode, IReadOnlyList<Guid>? CaseIds, string? IdempotencyKey)
    : ICommand<WriteBackJobDto>;

public sealed record RollbackWriteBackCommand(Guid JobId, string Reason) : ICommand<WriteBackJobDto>;

public sealed record GetWriteBackJobsQuery(int Page = 1, int? PageSize = null, string? SourceCode = null)
    : IQuery<PagedResult<WriteBackJobDto>>;

public sealed record GetWriteBackJobByIdQuery(Guid JobId) : IQuery<WriteBackJobDto>;

public sealed record ReconcileWriteBackQuery(Guid JobId) : IQuery<WriteBackReconciliationDto>;

public sealed record GetWriteBackTargetsQuery : IQuery<IReadOnlyList<WriteBackTargetDto>>;

public sealed class PreviewWriteBackCommandValidator : AbstractValidator<PreviewWriteBackCommand>
{
    public PreviewWriteBackCommandValidator() =>
        RuleFor(command => command.SourceCode).NotEmpty().MaximumLength(32);
}

public sealed class ApplyWriteBackCommandValidator : AbstractValidator<ApplyWriteBackCommand>
{
    public ApplyWriteBackCommandValidator()
    {
        RuleFor(command => command.SourceCode).NotEmpty().MaximumLength(32);
        RuleFor(command => command.IdempotencyKey).MaximumLength(128);
    }
}

public sealed class RollbackWriteBackCommandValidator : AbstractValidator<RollbackWriteBackCommand>
{
    public RollbackWriteBackCommandValidator()
    {
        RuleFor(command => command.JobId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(512);
    }
}

public sealed class PreviewWriteBackCommandHandler(WriteBackService service)
    : IRequestHandler<PreviewWriteBackCommand, Result<WriteBackPreviewDto>>
{
    public Task<Result<WriteBackPreviewDto>> HandleAsync(
        PreviewWriteBackCommand request,
        CancellationToken cancellationToken) =>
        service.PreviewAsync(request.SourceCode, request.CaseIds, cancellationToken);
}

public sealed class ApplyWriteBackCommandHandler(WriteBackService service)
    : IRequestHandler<ApplyWriteBackCommand, Result<WriteBackJobDto>>
{
    public Task<Result<WriteBackJobDto>> HandleAsync(
        ApplyWriteBackCommand request,
        CancellationToken cancellationToken) =>
        service.ApplyAsync(request.SourceCode, request.CaseIds, request.IdempotencyKey, cancellationToken);
}

public sealed class RollbackWriteBackCommandHandler(WriteBackService service)
    : IRequestHandler<RollbackWriteBackCommand, Result<WriteBackJobDto>>
{
    public Task<Result<WriteBackJobDto>> HandleAsync(
        RollbackWriteBackCommand request,
        CancellationToken cancellationToken) =>
        service.RollbackAsync(request.JobId, request.Reason, cancellationToken);
}

public sealed class ReconcileWriteBackQueryHandler(WriteBackService service)
    : IRequestHandler<ReconcileWriteBackQuery, Result<WriteBackReconciliationDto>>
{
    public Task<Result<WriteBackReconciliationDto>> HandleAsync(
        ReconcileWriteBackQuery request,
        CancellationToken cancellationToken) =>
        service.ReconcileAsync(request.JobId, cancellationToken);
}

public sealed class GetWriteBackJobsQueryHandler(
    IRemediationDbContext context,
    ISettingsReader settings,
    IClock clock)
    : IRequestHandler<GetWriteBackJobsQuery, Result<PagedResult<WriteBackJobDto>>>
{
    public async Task<Result<PagedResult<WriteBackJobDto>>> HandleAsync(
        GetWriteBackJobsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(
            request.PageSize ?? await settings.GetAsync(
                RemediationSettingKeys.PageSize,
                RemediationDefaults.PageSize,
                cancellationToken),
            1,
            RemediationDefaults.MaxPageSize);

        var page = Math.Max(request.Page, 1);
        var query = context.WriteBackJobs.Include(job => job.Items).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SourceCode))
        {
            var sourceCode = request.SourceCode.ToUpperInvariant();
            query = query.Where(job => job.TargetSourceCode == sourceCode);
        }

        var totalCount = await query.LongCountAsync(cancellationToken);

        var jobs = await query
            .OrderByDescending(job => job.RequestedAtUtc)
            .ThenBy(job => job.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<WriteBackJobDto>(
            [.. jobs.Select(job => job.ToDto())],
            page,
            pageSize,
            totalCount,
            clock.UtcNow);
    }
}

public sealed class GetWriteBackJobByIdQueryHandler(IRemediationDbContext context)
    : IRequestHandler<GetWriteBackJobByIdQuery, Result<WriteBackJobDto>>
{
    public async Task<Result<WriteBackJobDto>> HandleAsync(
        GetWriteBackJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var job = await context.WriteBackJobs
            .Include(entity => entity.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Id == request.JobId, cancellationToken);

        return job is null
            ? Result.Failure<WriteBackJobDto>(WriteBackErrors.JobNotFound(request.JobId))
            : job.ToDto();
    }
}

public sealed class GetWriteBackTargetsQueryHandler(IRemediationDbContext context)
    : IRequestHandler<GetWriteBackTargetsQuery, Result<IReadOnlyList<WriteBackTargetDto>>>
{
    public async Task<Result<IReadOnlyList<WriteBackTargetDto>>> HandleAsync(
        GetWriteBackTargetsQuery request,
        CancellationToken cancellationToken)
    {
        var targets = await context.WriteBackTargets
            .AsNoTracking()
            .OrderBy(target => target.SourceCode)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<WriteBackTargetDto>>([.. targets.Select(target => target.ToDto())]);
    }
}
