using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Simulation.Application.Abstractions;
using PDR.Simulation.Domain.Testing;

namespace PDR.Simulation.Application.Testing;

public sealed record TestCaseDto(
    Guid Id,
    string Reference,
    string Title,
    TestRisk Risk,
    string? ScenarioCode,
    string? SampleReference,
    string ExpectedResult,
    TestExecutionStatus Status,
    string? ActualResult,
    string? EvidenceReference,
    string? DefectReference,
    string? ExecutedBy,
    DateTimeOffset? ExecutedAtUtc,
    int ExecutionCount,
    bool IsRetested,
    UatOutcome UatOutcome,
    string? EngineOutcome,
    string? PlatformOutcome,
    string? UatExplanation,
    DateTimeOffset? ReconciledAtUtc);

public sealed record TestPlanDto(
    Guid Id,
    string Code,
    string Name,
    string Owner,
    string? Scope,
    string? Description,
    PlanStatus Status,
    int CaseCount,
    int PassedCount,
    int FailedCount,
    int BlockedCount,
    int NotRunCount,
    int OpenDefectCount,
    int UatMismatchCount,
    decimal RiskWeightedCoveragePercent,
    IReadOnlyList<TestCaseDto> Cases);

public sealed record CreateTestPlanCommand(string Code, string Name, string Owner, string? Scope, string? Description)
    : ICommand<TestPlanDto>;

public sealed record AddTestCaseCommand(
    string PlanCode,
    string Reference,
    string Title,
    TestRisk Risk,
    string? ScenarioCode,
    string? SampleReference,
    string ExpectedResult) : ICommand<TestPlanDto>;

public sealed record ActivateTestPlanCommand(string PlanCode) : ICommand<TestPlanDto>;

public sealed record CloseTestPlanCommand(string PlanCode) : ICommand<TestPlanDto>;

public sealed record RecordExecutionCommand(
    string PlanCode,
    string Reference,
    TestExecutionStatus Status,
    string ActualResult,
    string? EvidenceReference,
    string? DefectReference) : ICommand<TestPlanDto>;

/// <summary>Records what the payment engine did with a sample next to what the platform predicted (FR-TST-003).</summary>
public sealed record RecordUatOutcomeCommand(
    string PlanCode,
    string Reference,
    string EngineOutcome,
    string PlatformOutcome,
    string? Explanation) : ICommand<TestPlanDto>;

public sealed record GetTestPlansQuery : IQuery<IReadOnlyList<TestPlanDto>>;

public sealed record GetTestPlanQuery(string PlanCode) : IQuery<TestPlanDto>;

public sealed class CreateTestPlanCommandValidator : AbstractValidator<CreateTestPlanCommand>
{
    public CreateTestPlanCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(140);
        RuleFor(command => command.Owner).NotEmpty().MaximumLength(140);
    }
}

public sealed class AddTestCaseCommandValidator : AbstractValidator<AddTestCaseCommand>
{
    public AddTestCaseCommandValidator()
    {
        RuleFor(command => command.PlanCode).NotEmpty();
        RuleFor(command => command.Reference).NotEmpty().MaximumLength(64);
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ExpectedResult).NotEmpty().MaximumLength(512);
    }
}

public sealed class RecordExecutionCommandValidator : AbstractValidator<RecordExecutionCommand>
{
    public RecordExecutionCommandValidator()
    {
        RuleFor(command => command.PlanCode).NotEmpty();
        RuleFor(command => command.Reference).NotEmpty();
        RuleFor(command => command.ActualResult).NotEmpty().MaximumLength(1024);
    }
}

public sealed class RecordUatOutcomeCommandValidator : AbstractValidator<RecordUatOutcomeCommand>
{
    public RecordUatOutcomeCommandValidator()
    {
        RuleFor(command => command.PlanCode).NotEmpty();
        RuleFor(command => command.Reference).NotEmpty();
        RuleFor(command => command.EngineOutcome).NotEmpty().MaximumLength(140);
        RuleFor(command => command.PlatformOutcome).NotEmpty().MaximumLength(140);
    }
}

public static class TestPlanMapper
{
    public static TestPlanDto ToDto(this TestPlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Owner,
            plan.Scope,
            plan.Description,
            plan.Status,
            plan.Cases.Count,
            plan.PassedCount,
            plan.FailedCount,
            plan.BlockedCount,
            plan.NotRunCount,
            plan.OpenDefectCount,
            plan.Cases.Count(item => item.UatOutcome == UatOutcome.Mismatch),
            plan.RiskWeightedCoveragePercent,
            [
                .. plan.Cases
                    .OrderByDescending(item => item.Risk)
                    .ThenBy(item => item.Reference)
                    .Select(item => new TestCaseDto(
                        item.Id,
                        item.Reference,
                        item.Title,
                        item.Risk,
                        item.ScenarioCode,
                        item.SampleReference,
                        item.ExpectedResult,
                        item.Status,
                        item.ActualResult,
                        item.EvidenceReference,
                        item.DefectReference,
                        item.ExecutedBy,
                        item.ExecutedAtUtc,
                        item.ExecutionCount,
                        item.IsRetested,
                        item.UatOutcome,
                        item.EngineOutcome,
                        item.PlatformOutcome,
                        item.UatExplanation,
                        item.ReconciledAtUtc))
            ]);
}

internal static class TestPlanLoader
{
    public static Task<TestPlan?> LoadAsync(ISimulationDbContext context, string planCode, CancellationToken cancellationToken)
    {
        var code = planCode.ToUpperInvariant();
        return context.TestPlans
            .Include(plan => plan.Cases)
            .FirstOrDefaultAsync(plan => plan.Code == code, cancellationToken);
    }
}

public sealed class CreateTestPlanCommandHandler(ISimulationDbContext context)
    : IRequestHandler<CreateTestPlanCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(CreateTestPlanCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.ToUpperInvariant();
        if (await context.TestPlans.AnyAsync(plan => plan.Code == code, cancellationToken))
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.Duplicate(code));
        }

        var plan = TestPlan.Create(code, request.Name, request.Owner, request.Scope, request.Description);
        context.TestPlans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class AddTestCaseCommandHandler(ISimulationDbContext context)
    : IRequestHandler<AddTestCaseCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(AddTestCaseCommand request, CancellationToken cancellationToken)
    {
        var plan = await TestPlanLoader.LoadAsync(context, request.PlanCode, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(request.PlanCode.ToUpperInvariant()));
        }

        var added = plan.AddCase(
            request.Reference,
            request.Title,
            request.Risk,
            request.ScenarioCode,
            request.SampleReference,
            request.ExpectedResult);

        if (added.IsFailure)
        {
            return Result.Failure<TestPlanDto>(added.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class ActivateTestPlanCommandHandler(ISimulationDbContext context)
    : IRequestHandler<ActivateTestPlanCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(ActivateTestPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await TestPlanLoader.LoadAsync(context, request.PlanCode, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(request.PlanCode.ToUpperInvariant()));
        }

        var activated = plan.Activate();
        if (activated.IsFailure)
        {
            return Result.Failure<TestPlanDto>(activated.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class CloseTestPlanCommandHandler(ISimulationDbContext context)
    : IRequestHandler<CloseTestPlanCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(CloseTestPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await TestPlanLoader.LoadAsync(context, request.PlanCode, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(request.PlanCode.ToUpperInvariant()));
        }

        var closed = plan.Close();
        if (closed.IsFailure)
        {
            return Result.Failure<TestPlanDto>(closed.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class RecordExecutionCommandHandler(ISimulationDbContext context, ICurrentUser currentUser, IClock clock)
    : IRequestHandler<RecordExecutionCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(RecordExecutionCommand request, CancellationToken cancellationToken)
    {
        var plan = await TestPlanLoader.LoadAsync(context, request.PlanCode, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(request.PlanCode.ToUpperInvariant()));
        }

        var recorded = plan.RecordExecution(
            request.Reference,
            request.Status,
            request.ActualResult,
            request.EvidenceReference,
            request.DefectReference,
            currentUser.UserName,
            clock.UtcNow);

        if (recorded.IsFailure)
        {
            return Result.Failure<TestPlanDto>(recorded.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class RecordUatOutcomeCommandHandler(ISimulationDbContext context, IClock clock)
    : IRequestHandler<RecordUatOutcomeCommand, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(RecordUatOutcomeCommand request, CancellationToken cancellationToken)
    {
        var plan = await TestPlanLoader.LoadAsync(context, request.PlanCode, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(request.PlanCode.ToUpperInvariant()));
        }

        var recorded = plan.RecordUatOutcome(
            request.Reference,
            request.EngineOutcome,
            request.PlatformOutcome,
            request.Explanation,
            clock.UtcNow);

        if (recorded.IsFailure)
        {
            return Result.Failure<TestPlanDto>(recorded.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return plan.ToDto();
    }
}

public sealed class GetTestPlansQueryHandler(ISimulationDbContext context)
    : IRequestHandler<GetTestPlansQuery, Result<IReadOnlyList<TestPlanDto>>>
{
    public async Task<Result<IReadOnlyList<TestPlanDto>>> HandleAsync(
        GetTestPlansQuery request,
        CancellationToken cancellationToken)
    {
        var plans = await context.TestPlans
            .AsNoTracking()
            .Include(plan => plan.Cases)
            .OrderBy(plan => plan.Code)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TestPlanDto>>([.. plans.Select(plan => plan.ToDto())]);
    }
}

public sealed class GetTestPlanQueryHandler(ISimulationDbContext context)
    : IRequestHandler<GetTestPlanQuery, Result<TestPlanDto>>
{
    public async Task<Result<TestPlanDto>> HandleAsync(GetTestPlanQuery request, CancellationToken cancellationToken)
    {
        var code = request.PlanCode.ToUpperInvariant();
        var plan = await context.TestPlans
            .AsNoTracking()
            .Include(entity => entity.Cases)
            .FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

        return plan is null ? Result.Failure<TestPlanDto>(TestPlanErrors.NotFound(code)) : plan.ToDto();
    }
}
