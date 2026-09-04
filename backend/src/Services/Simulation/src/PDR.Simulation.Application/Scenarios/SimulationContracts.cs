using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.Application.Scenarios;

public sealed record ScenarioDto(
    Guid Id,
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
    string? Description,
    ScenarioStatus Status,
    int RunCount,
    DateTimeOffset? LastRunAtUtc);

public sealed record SimulationRunDto(
    Guid Id,
    Guid ScenarioId,
    string ScenarioCode,
    ScenarioMode Mode,
    DateOnly AsOf,
    string RunKey,
    string RequestedBy,
    RunStatus Status,
    int PopulationCount,
    int AssessedCount,
    int ExcludedCount,
    int UnableToAssessCount,
    int RejectedCount,
    int WarningCount,
    int PaymentsAtRisk,
    decimal ReadinessPercent,
    bool Reconciles,
    string? RulesetVersion,
    string? FailureReason,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<SimulationBreakdownDto> Breakdown);

public sealed record SimulationBreakdownDto(
    BreakdownDimension Dimension,
    string Key,
    int RecordCount,
    int RejectedCount,
    int WarningCount,
    int PaymentsAtRisk,
    decimal ReadinessPercent);

/// <summary>Two stored runs side by side, with the deltas a reader would otherwise compute by hand (FR-SIM-002).</summary>
public sealed record RunComparisonDto(
    SimulationRunDto Baseline,
    SimulationRunDto Candidate,
    bool SameRunKey,
    int RejectedDelta,
    int PaymentsAtRiskDelta,
    decimal ReadinessDelta,
    IReadOnlyList<ComparisonRowDto> Rows);

public sealed record ComparisonRowDto(
    BreakdownDimension Dimension,
    string Key,
    int BaselineRejected,
    int CandidateRejected,
    int RejectedDelta);

public static class SimulationSettingKeys
{
    public const string PageSize = "simulation.page-size";
    public const string DefaultCutoverDate = "simulation.default-cutover-date";
    public const string ResidualExposureTolerance = "simulation.residual-exposure-tolerance";
}

public static class SimulationDefaults
{
    public const int PageSize = 20;
    public const int MaxPageSize = 200;
    public const string DefaultCutoverDate = "2026-11-22";

    /// <summary>Rejections still allowed at go-live before the pack turns into a no-go (FR-CUT-004).</summary>
    public const int ResidualExposureTolerance = 0;
}

public static class SimulationMapper
{
    public static ScenarioDto ToDto(this Scenario scenario, int runCount = 0, DateTimeOffset? lastRunAtUtc = null) =>
        new(
            scenario.Id,
            scenario.Code,
            scenario.Name,
            scenario.Mode,
            scenario.AsOf,
            scenario.SchemeCodes,
            scenario.SourceCodes,
            scenario.Countries,
            scenario.PartyRoles,
            scenario.Exclusions,
            scenario.RulesetVersion,
            scenario.Description,
            scenario.Status,
            runCount,
            lastRunAtUtc);

    public static SimulationRunDto ToDto(this SimulationRun run) =>
        new(
            run.Id,
            run.ScenarioId,
            run.ScenarioCode,
            run.Mode,
            run.AsOf,
            run.RunKey,
            run.RequestedBy,
            run.Status,
            run.PopulationCount,
            run.AssessedCount,
            run.ExcludedCount,
            run.UnableToAssessCount,
            run.RejectedCount,
            run.WarningCount,
            run.PaymentsAtRisk,
            run.ReadinessPercent,
            run.Reconciles,
            run.RulesetVersion,
            run.FailureReason,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            [
                .. run.Breakdown
                    .OrderBy(row => row.Dimension)
                    .ThenByDescending(row => row.RejectedCount)
                    .Select(row => new SimulationBreakdownDto(
                        row.Dimension,
                        row.Key,
                        row.RecordCount,
                        row.RejectedCount,
                        row.WarningCount,
                        row.PaymentsAtRisk,
                        row.ReadinessPercent))
            ]);
}
