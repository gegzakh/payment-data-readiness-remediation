using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Security;
using PDR.Simulation.Application.Abstractions;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.Application.Scenarios;

public sealed record CreateScenarioCommand(
    string Code,
    string Name,
    ScenarioMode Mode,
    DateOnly AsOf,
    string? SchemeCodes,
    string? SourceCodes,
    string? Countries,
    string? PartyRoles,
    string? Exclusions,
    string? RulesetVersion,
    string? Description) : ICommand<ScenarioDto>;

public sealed record UpdateScenarioCommand(
    string Code,
    string Name,
    DateOnly AsOf,
    string? SchemeCodes,
    string? SourceCodes,
    string? Countries,
    string? PartyRoles,
    string? Exclusions,
    string? RulesetVersion,
    string? Description) : ICommand<ScenarioDto>;

public sealed record LockScenarioCommand(string Code) : ICommand<ScenarioDto>;

public sealed record ArchiveScenarioCommand(string Code) : ICommand<ScenarioDto>;

public sealed record GetScenariosQuery(ScenarioMode? Mode = null, bool IncludeArchived = false)
    : IQuery<IReadOnlyList<ScenarioDto>>;

public sealed record GetScenarioQuery(string Code) : IQuery<ScenarioDto>;

/// <summary>Executes a scenario against the live portfolio and stores the result (FR-SIM-001).</summary>
public sealed record RunScenarioCommand(string Code) : ICommand<SimulationRunDto>;

public sealed record GetRunsQuery(string? ScenarioCode = null, int Page = 1, int? PageSize = null)
    : IQuery<PagedResult<SimulationRunDto>>;

public sealed record GetRunQuery(Guid RunId) : IQuery<SimulationRunDto>;

public sealed record CompareRunsQuery(Guid BaselineRunId, Guid CandidateRunId) : IQuery<RunComparisonDto>;

public sealed class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Description).MaximumLength(1024);
        RuleFor(command => command.Exclusions).MaximumLength(512);
    }
}

public sealed class UpdateScenarioCommandValidator : AbstractValidator<UpdateScenarioCommand>
{
    public UpdateScenarioCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
    }
}

internal static class SimulationPageSize
{
    public static async Task<int> ResolveAsync(ISettingsReader settings, int? requested, CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            SimulationSettingKeys.PageSize,
            SimulationDefaults.PageSize,
            cancellationToken);

        return Math.Clamp(requested ?? configured, 1, SimulationDefaults.MaxPageSize);
    }
}

public sealed class CreateScenarioCommandHandler(ISimulationDbContext context)
    : IRequestHandler<CreateScenarioCommand, Result<ScenarioDto>>
{
    public async Task<Result<ScenarioDto>> HandleAsync(CreateScenarioCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.Scenarios.AnyAsync(scenario => scenario.Code == code, cancellationToken))
        {
            return Result.Failure<ScenarioDto>(ScenarioErrors.Duplicate(code));
        }

        var scenario = Scenario.Create(
            code,
            request.Name,
            request.Mode,
            request.AsOf,
            request.SchemeCodes,
            request.SourceCodes,
            request.Countries,
            request.PartyRoles,
            request.Exclusions,
            request.RulesetVersion,
            request.Description);

        context.Scenarios.Add(scenario);
        await context.SaveChangesAsync(cancellationToken);
        return scenario.ToDto();
    }
}

public sealed class UpdateScenarioCommandHandler(ISimulationDbContext context)
    : IRequestHandler<UpdateScenarioCommand, Result<ScenarioDto>>
{
    public async Task<Result<ScenarioDto>> HandleAsync(UpdateScenarioCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scenario = await context.Scenarios.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (scenario is null)
        {
            return Result.Failure<ScenarioDto>(ScenarioErrors.NotFound(code));
        }

        var update = scenario.Update(
            request.Name,
            request.AsOf,
            request.SchemeCodes,
            request.SourceCodes,
            request.Countries,
            request.PartyRoles,
            request.Exclusions,
            request.RulesetVersion,
            request.Description);

        if (update.IsFailure)
        {
            return Result.Failure<ScenarioDto>(update.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return scenario.ToDto();
    }
}

public sealed class LockScenarioCommandHandler(ISimulationDbContext context)
    : IRequestHandler<LockScenarioCommand, Result<ScenarioDto>>
{
    public async Task<Result<ScenarioDto>> HandleAsync(LockScenarioCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scenario = await context.Scenarios.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (scenario is null)
        {
            return Result.Failure<ScenarioDto>(ScenarioErrors.NotFound(code));
        }

        var locked = scenario.Lock();
        if (locked.IsFailure)
        {
            return Result.Failure<ScenarioDto>(locked.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return scenario.ToDto();
    }
}

public sealed class ArchiveScenarioCommandHandler(ISimulationDbContext context)
    : IRequestHandler<ArchiveScenarioCommand, Result<ScenarioDto>>
{
    public async Task<Result<ScenarioDto>> HandleAsync(ArchiveScenarioCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scenario = await context.Scenarios.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (scenario is null)
        {
            return Result.Failure<ScenarioDto>(ScenarioErrors.NotFound(code));
        }

        scenario.Archive();
        await context.SaveChangesAsync(cancellationToken);
        return scenario.ToDto();
    }
}

public sealed class GetScenariosQueryHandler(ISimulationDbContext context)
    : IRequestHandler<GetScenariosQuery, Result<IReadOnlyList<ScenarioDto>>>
{
    public async Task<Result<IReadOnlyList<ScenarioDto>>> HandleAsync(
        GetScenariosQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Scenarios.AsNoTracking();

        if (request.Mode is { } mode)
        {
            query = query.Where(scenario => scenario.Mode == mode);
        }

        if (!request.IncludeArchived)
        {
            query = query.Where(scenario => scenario.Status != ScenarioStatus.Archived);
        }

        var scenarios = await query.OrderBy(scenario => scenario.Code).ToListAsync(cancellationToken);

        var runs = await context.Runs
            .AsNoTracking()
            .GroupBy(run => run.ScenarioId)
            .Select(group => new { ScenarioId = group.Key, Count = group.Count(), Last = group.Max(run => run.StartedAtUtc) })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ScenarioDto>>(
        [
            .. scenarios.Select(scenario =>
            {
                var stats = runs.FirstOrDefault(item => item.ScenarioId == scenario.Id);
                return scenario.ToDto(stats?.Count ?? 0, stats?.Last);
            })
        ]);
    }
}

public sealed class GetScenarioQueryHandler(ISimulationDbContext context)
    : IRequestHandler<GetScenarioQuery, Result<ScenarioDto>>
{
    public async Task<Result<ScenarioDto>> HandleAsync(GetScenarioQuery request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scenario = await context.Scenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

        return scenario is null
            ? Result.Failure<ScenarioDto>(ScenarioErrors.NotFound(code))
            : scenario.ToDto();
    }
}

public sealed class RunScenarioCommandHandler(ISimulationDbContext context, SimulationRunner runner, ICurrentUser currentUser)
    : IRequestHandler<RunScenarioCommand, Result<SimulationRunDto>>
{
    public async Task<Result<SimulationRunDto>> HandleAsync(RunScenarioCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        var scenario = await context.Scenarios.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);
        if (scenario is null)
        {
            return Result.Failure<SimulationRunDto>(ScenarioErrors.NotFound(code));
        }

        var executed = await runner.ExecuteAsync(scenario, currentUser.UserName, cancellationToken);
        if (executed.IsFailure)
        {
            return Result.Failure<SimulationRunDto>(executed.Error);
        }

        context.Runs.Add(executed.Value);
        await context.SaveChangesAsync(cancellationToken);
        return executed.Value.ToDto();
    }
}

public sealed class GetRunsQueryHandler(ISimulationDbContext context, ISettingsReader settings)
    : IRequestHandler<GetRunsQuery, Result<PagedResult<SimulationRunDto>>>
{
    public async Task<Result<PagedResult<SimulationRunDto>>> HandleAsync(
        GetRunsQuery request,
        CancellationToken cancellationToken)
    {
        var pageSize = await SimulationPageSize.ResolveAsync(settings, request.PageSize, cancellationToken);
        var page = Math.Max(request.Page, 1);

        var query = context.Runs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.ScenarioCode))
        {
            var code = request.ScenarioCode.ToUpperInvariant();
            query = query.Where(run => run.ScenarioCode == code);
        }

        var total = await query.CountAsync(cancellationToken);
        var runs = await query
            .OrderByDescending(run => run.StartedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = runs.Select(run => run.ToDto()).ToList();
        return Result.Success(new PagedResult<SimulationRunDto>(items, page, pageSize, total, DateTimeOffset.UtcNow));
    }
}

public sealed class GetRunQueryHandler(ISimulationDbContext context)
    : IRequestHandler<GetRunQuery, Result<SimulationRunDto>>
{
    public async Task<Result<SimulationRunDto>> HandleAsync(GetRunQuery request, CancellationToken cancellationToken)
    {
        var run = await context.Runs
            .AsNoTracking()
            .Include(entity => entity.Breakdown)
            .FirstOrDefaultAsync(entity => entity.Id == request.RunId, cancellationToken);

        return run is null
            ? Result.Failure<SimulationRunDto>(SimulationRunErrors.NotFound(request.RunId))
            : run.ToDto();
    }
}

public sealed class CompareRunsQueryHandler(ISimulationDbContext context)
    : IRequestHandler<CompareRunsQuery, Result<RunComparisonDto>>
{
    public async Task<Result<RunComparisonDto>> HandleAsync(CompareRunsQuery request, CancellationToken cancellationToken)
    {
        var baseline = await LoadAsync(request.BaselineRunId, cancellationToken);
        if (baseline is null)
        {
            return Result.Failure<RunComparisonDto>(SimulationRunErrors.NotFound(request.BaselineRunId));
        }

        var candidate = await LoadAsync(request.CandidateRunId, cancellationToken);
        if (candidate is null)
        {
            return Result.Failure<RunComparisonDto>(SimulationRunErrors.NotFound(request.CandidateRunId));
        }

        if (baseline.Status != RunStatus.Completed || candidate.Status != RunStatus.Completed)
        {
            return Result.Failure<RunComparisonDto>(SimulationRunErrors.NotComparable);
        }

        var baselineRows = baseline.Breakdown.ToDictionary(row => (row.Dimension, row.Key), row => row.RejectedCount);
        var candidateRows = candidate.Breakdown.ToDictionary(row => (row.Dimension, row.Key), row => row.RejectedCount);

        // A row missing on one side counts as zero there, so a dimension that appears or disappears is visible.
        var rows = baselineRows.Keys
            .Union(candidateRows.Keys)
            .Select(key =>
            {
                var baselineRejected = baselineRows.GetValueOrDefault(key);
                var candidateRejected = candidateRows.GetValueOrDefault(key);
                return new ComparisonRowDto(
                    key.Dimension,
                    key.Key,
                    baselineRejected,
                    candidateRejected,
                    candidateRejected - baselineRejected);
            })
            .OrderBy(row => row.Dimension)
            .ThenBy(row => row.Key)
            .ToList();

        return new RunComparisonDto(
            baseline.ToDto(),
            candidate.ToDto(),
            string.Equals(baseline.RunKey, candidate.RunKey, StringComparison.Ordinal),
            candidate.RejectedCount - baseline.RejectedCount,
            candidate.PaymentsAtRisk - baseline.PaymentsAtRisk,
            candidate.ReadinessPercent - baseline.ReadinessPercent,
            rows);
    }

    private Task<SimulationRun?> LoadAsync(Guid runId, CancellationToken cancellationToken) =>
        context.Runs
            .AsNoTracking()
            .Include(entity => entity.Breakdown)
            .FirstOrDefaultAsync(entity => entity.Id == runId, cancellationToken);
}
