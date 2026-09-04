using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Simulation.Domain.Scenarios;

/// <summary>
/// One execution of a scenario. The totals are stored rather than recomputed so a result stays
/// reproducible after the underlying portfolio moves on (FR-SIM-002), and they must reconcile:
/// the population is assessed plus excluded plus unable-to-assess.
/// </summary>
public sealed class SimulationRun : AggregateRoot
{
    private readonly List<SimulationBreakdown> _breakdown = [];

    private SimulationRun()
    {
    }

    private SimulationRun(Guid scenarioId, string scenarioCode, ScenarioMode mode, DateOnly asOf, string runKey, string requestedBy, DateTimeOffset startedAtUtc)
    {
        ScenarioId = scenarioId;
        ScenarioCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(scenarioCode), 32);
        Mode = mode;
        AsOf = asOf;
        RunKey = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(runKey), 128);
        RequestedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(requestedBy), 140);
        StartedAtUtc = startedAtUtc;
        Status = RunStatus.Running;
    }

    public Guid ScenarioId { get; private set; }

    public string ScenarioCode { get; private set; } = string.Empty;

    public ScenarioMode Mode { get; private set; }

    public DateOnly AsOf { get; private set; }

    /// <summary>Scenario definition plus ruleset, so an identical re-run is recognisable (FR-SIM-002).</summary>
    public string RunKey { get; private set; } = string.Empty;

    public string RequestedBy { get; private set; } = string.Empty;

    public RunStatus Status { get; private set; }

    public int PopulationCount { get; private set; }

    public int AssessedCount { get; private set; }

    public int ExcludedCount { get; private set; }

    public int UnableToAssessCount { get; private set; }

    public int RejectedCount { get; private set; }

    public int WarningCount { get; private set; }

    public int PaymentsAtRisk { get; private set; }

    public decimal ReadinessPercent { get; private set; }

    public string? RulesetVersion { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<SimulationBreakdown> Breakdown => _breakdown.AsReadOnly();

    /// <summary>The population must add up; a run that does not reconcile is not evidence (FR-SIM-001).</summary>
    public bool Reconciles => PopulationCount == AssessedCount + ExcludedCount + UnableToAssessCount;

    public static SimulationRun Start(
        Guid scenarioId,
        string scenarioCode,
        ScenarioMode mode,
        DateOnly asOf,
        string runKey,
        string requestedBy,
        DateTimeOffset startedAtUtc) =>
        new(scenarioId, scenarioCode, mode, asOf, runKey, requestedBy, startedAtUtc);

    public Result Complete(
        int populationCount,
        int assessedCount,
        int excludedCount,
        int unableToAssessCount,
        int rejectedCount,
        int warningCount,
        int paymentsAtRisk,
        string? rulesetVersion,
        DateTimeOffset completedAtUtc)
    {
        if (Status != RunStatus.Running)
        {
            return Result.Failure(SimulationRunErrors.NotRunning(Status));
        }

        PopulationCount = Math.Max(populationCount, 0);
        AssessedCount = Math.Max(assessedCount, 0);
        ExcludedCount = Math.Max(excludedCount, 0);
        UnableToAssessCount = Math.Max(unableToAssessCount, 0);
        RejectedCount = Math.Clamp(rejectedCount, 0, AssessedCount);
        WarningCount = Math.Max(warningCount, 0);
        PaymentsAtRisk = Math.Max(paymentsAtRisk, 0);
        RulesetVersion = rulesetVersion is null ? null : Ensure.MaxLength(rulesetVersion, 32);
        ReadinessPercent = AssessedCount == 0
            ? 0m
            : Math.Round((AssessedCount - RejectedCount) * 100m / AssessedCount, 2);
        Status = RunStatus.Completed;
        CompletedAtUtc = completedAtUtc;
        return Result.Success();
    }

    public void Fail(string reason, DateTimeOffset atUtc)
    {
        Status = RunStatus.Failed;
        FailureReason = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reason), 512);
        CompletedAtUtc = atUtc;
    }

    public void AddBreakdown(BreakdownDimension dimension, string key, int recordCount, int rejectedCount, int warningCount, int paymentsAtRisk) =>
        _breakdown.Add(new SimulationBreakdown(Id, dimension, key, recordCount, rejectedCount, warningCount, paymentsAtRisk));
}

/// <summary>One cut of a stored run, kept with the run so results survive later portfolio changes.</summary>
public sealed class SimulationBreakdown : Entity
{
    private SimulationBreakdown()
    {
    }

    internal SimulationBreakdown(
        Guid runId,
        BreakdownDimension dimension,
        string key,
        int recordCount,
        int rejectedCount,
        int warningCount,
        int paymentsAtRisk)
    {
        RunId = runId;
        Dimension = dimension;
        Key = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(key), 140);
        RecordCount = Math.Max(recordCount, 0);
        RejectedCount = Math.Clamp(rejectedCount, 0, RecordCount);
        WarningCount = Math.Max(warningCount, 0);
        PaymentsAtRisk = Math.Max(paymentsAtRisk, 0);
    }

    public Guid RunId { get; private set; }

    public BreakdownDimension Dimension { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public int RecordCount { get; private set; }

    public int RejectedCount { get; private set; }

    public int WarningCount { get; private set; }

    public int PaymentsAtRisk { get; private set; }

    public decimal ReadinessPercent =>
        RecordCount == 0 ? 0m : Math.Round((RecordCount - RejectedCount) * 100m / RecordCount, 2);
}

public static class SimulationRunErrors
{
    public static Error NotFound(Guid runId) =>
        Error.NotFound("SIMULATION.RUN_NOT_FOUND", $"Simulation run '{runId}' was not found.");

    public static Error NotRunning(RunStatus status) =>
        Error.Conflict("SIMULATION.RUN_NOT_RUNNING", $"A run in state '{status}' can no longer be completed.");

    public static readonly Error NotComparable =
        Error.Conflict("SIMULATION.NOT_COMPARABLE", "Only completed runs can be compared.");
}
