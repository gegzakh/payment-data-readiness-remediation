using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Simulation.Domain.Testing;

public enum TestRisk
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum TestExecutionStatus
{
    NotRun = 0,
    Passed = 1,
    Failed = 2,
    Blocked = 3
}

public enum PlanStatus
{
    Draft = 0,
    Active = 1,
    Closed = 2
}

/// <summary>Whether the platform and the payment engine agreed about a sample (FR-TST-003).</summary>
public enum UatOutcome
{
    NotCompared = 0,
    Match = 1,
    Mismatch = 2
}

/// <summary>
/// A risk-based test plan: which scenarios and samples are exercised, what each is expected to do, and
/// what actually happened, including defects and retests (FR-TST-001).
/// </summary>
public sealed class TestPlan : AggregateRoot
{
    private readonly List<TestCase> _cases = [];

    private TestPlan()
    {
    }

    private TestPlan(string code, string name, string owner, string? scope, string? description)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        Owner = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(owner), 140);
        Scope = scope is null ? null : Ensure.MaxLength(scope, 512);
        Description = description is null ? null : Ensure.MaxLength(description, 1024);
        Status = PlanStatus.Draft;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Owner { get; private set; } = string.Empty;

    public string? Scope { get; private set; }

    public string? Description { get; private set; }

    public PlanStatus Status { get; private set; }

    public IReadOnlyCollection<TestCase> Cases => _cases.AsReadOnly();

    public int PassedCount => _cases.Count(item => item.Status == TestExecutionStatus.Passed);

    public int FailedCount => _cases.Count(item => item.Status == TestExecutionStatus.Failed);

    public int BlockedCount => _cases.Count(item => item.Status == TestExecutionStatus.Blocked);

    public int NotRunCount => _cases.Count(item => item.Status == TestExecutionStatus.NotRun);

    public int OpenDefectCount => _cases.Count(item => item.Status == TestExecutionStatus.Failed && !item.IsRetested);

    /// <summary>Coverage weighted by risk: a critical case left unrun holds the plan back much harder.</summary>
    public decimal RiskWeightedCoveragePercent
    {
        get
        {
            var totalWeight = _cases.Sum(item => Weight(item.Risk));
            if (totalWeight == 0)
            {
                return 0m;
            }

            var executedWeight = _cases
                .Where(item => item.Status is TestExecutionStatus.Passed or TestExecutionStatus.Failed)
                .Sum(item => Weight(item.Risk));

            return Math.Round(executedWeight * 100m / totalWeight, 2);
        }
    }

    public static TestPlan Create(string code, string name, string owner, string? scope, string? description) =>
        new(code, name, owner, scope, description);

    public Result<TestCase> AddCase(
        string reference,
        string title,
        TestRisk risk,
        string? scenarioCode,
        string? sampleReference,
        string expectedResult)
    {
        if (Status == PlanStatus.Closed)
        {
            return Result.Failure<TestCase>(TestPlanErrors.Closed(Code));
        }

        if (_cases.Any(item => string.Equals(item.Reference, reference, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<TestCase>(TestPlanErrors.DuplicateCase(reference));
        }

        var testCase = new TestCase(Id, reference, title, risk, scenarioCode, sampleReference, expectedResult);
        _cases.Add(testCase);
        return Result.Success(testCase);
    }

    public Result Activate()
    {
        if (_cases.Count == 0)
        {
            return Result.Failure(TestPlanErrors.Empty(Code));
        }

        Status = PlanStatus.Active;
        return Result.Success();
    }

    public Result Close()
    {
        if (OpenDefectCount > 0)
        {
            return Result.Failure(TestPlanErrors.OpenDefects(Code, OpenDefectCount));
        }

        Status = PlanStatus.Closed;
        return Result.Success();
    }

    public Result RecordExecution(
        string reference,
        TestExecutionStatus status,
        string actualResult,
        string? evidenceReference,
        string? defectReference,
        string executedBy,
        DateTimeOffset atUtc)
    {
        if (Status == PlanStatus.Closed)
        {
            return Result.Failure(TestPlanErrors.Closed(Code));
        }

        var testCase = _cases.FirstOrDefault(item => string.Equals(item.Reference, reference, StringComparison.OrdinalIgnoreCase));
        if (testCase is null)
        {
            return Result.Failure(TestPlanErrors.CaseNotFound(reference));
        }

        return testCase.Execute(status, actualResult, evidenceReference, defectReference, executedBy, atUtc);
    }

    public Result RecordUatOutcome(
        string reference,
        string engineOutcome,
        string platformOutcome,
        string? explanation,
        DateTimeOffset atUtc)
    {
        var testCase = _cases.FirstOrDefault(item => string.Equals(item.Reference, reference, StringComparison.OrdinalIgnoreCase));
        if (testCase is null)
        {
            return Result.Failure(TestPlanErrors.CaseNotFound(reference));
        }

        testCase.Reconcile(engineOutcome, platformOutcome, explanation, atUtc);
        return Result.Success();
    }

    private static int Weight(TestRisk risk) => risk switch
    {
        TestRisk.Critical => 8,
        TestRisk.High => 4,
        TestRisk.Medium => 2,
        _ => 1
    };
}

public sealed class TestCase : Entity
{
    private TestCase()
    {
    }

    internal TestCase(
        Guid planId,
        string reference,
        string title,
        TestRisk risk,
        string? scenarioCode,
        string? sampleReference,
        string expectedResult)
    {
        PlanId = planId;
        Reference = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(reference), 64).ToUpperInvariant();
        Title = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(title), 200);
        Risk = risk;
        ScenarioCode = scenarioCode is null ? null : Ensure.MaxLength(scenarioCode, 32).ToUpperInvariant();
        SampleReference = sampleReference is null ? null : Ensure.MaxLength(sampleReference, 140);
        ExpectedResult = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(expectedResult), 512);
        Status = TestExecutionStatus.NotRun;
    }

    public Guid PlanId { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public TestRisk Risk { get; private set; }

    public string? ScenarioCode { get; private set; }

    public string? SampleReference { get; private set; }

    public string ExpectedResult { get; private set; } = string.Empty;

    public TestExecutionStatus Status { get; private set; }

    public string? ActualResult { get; private set; }

    public string? EvidenceReference { get; private set; }

    public string? DefectReference { get; private set; }

    public string? ExecutedBy { get; private set; }

    public DateTimeOffset? ExecutedAtUtc { get; private set; }

    public int ExecutionCount { get; private set; }

    /// <summary>A case that failed and was executed again; the defect stays visible in its history.</summary>
    public bool IsRetested => ExecutionCount > 1 && Status == TestExecutionStatus.Passed;

    public UatOutcome UatOutcome { get; private set; }

    public string? EngineOutcome { get; private set; }

    public string? PlatformOutcome { get; private set; }

    public string? UatExplanation { get; private set; }

    public DateTimeOffset? ReconciledAtUtc { get; private set; }

    internal Result Execute(
        TestExecutionStatus status,
        string actualResult,
        string? evidenceReference,
        string? defectReference,
        string executedBy,
        DateTimeOffset atUtc)
    {
        if (status == TestExecutionStatus.NotRun)
        {
            return Result.Failure(TestPlanErrors.NotAnExecution);
        }

        if (status == TestExecutionStatus.Failed && string.IsNullOrWhiteSpace(defectReference))
        {
            return Result.Failure(TestPlanErrors.DefectRequired(Reference));
        }

        Status = status;
        ActualResult = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(actualResult), 1024);
        EvidenceReference = evidenceReference is null ? EvidenceReference : Ensure.MaxLength(evidenceReference, 512);
        DefectReference = defectReference is null ? DefectReference : Ensure.MaxLength(defectReference, 140);
        ExecutedBy = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(executedBy), 140);
        ExecutedAtUtc = atUtc;
        ExecutionCount++;
        return Result.Success();
    }

    internal void Reconcile(string engineOutcome, string platformOutcome, string? explanation, DateTimeOffset atUtc)
    {
        EngineOutcome = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(engineOutcome), 140);
        PlatformOutcome = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(platformOutcome), 140);
        UatExplanation = explanation is null ? null : Ensure.MaxLength(explanation, 1024);
        UatOutcome = string.Equals(EngineOutcome, PlatformOutcome, StringComparison.OrdinalIgnoreCase)
            ? UatOutcome.Match
            : UatOutcome.Mismatch;
        ReconciledAtUtc = atUtc;
    }
}

public static class TestPlanErrors
{
    public static Error NotFound(string code) =>
        Error.NotFound("TESTPLAN.NOT_FOUND", $"Test plan '{code}' was not found.");

    public static Error Duplicate(string code) =>
        Error.Conflict("TESTPLAN.DUPLICATE", $"Test plan '{code}' already exists.");

    public static Error DuplicateCase(string reference) =>
        Error.Conflict("TESTPLAN.DUPLICATE_CASE", $"Test case '{reference}' already exists in this plan.");

    public static Error CaseNotFound(string reference) =>
        Error.NotFound("TESTPLAN.CASE_NOT_FOUND", $"Test case '{reference}' was not found in this plan.");

    public static Error Closed(string code) =>
        Error.Conflict("TESTPLAN.CLOSED", $"Test plan '{code}' is closed.");

    public static Error Empty(string code) =>
        Error.Conflict("TESTPLAN.EMPTY", $"Test plan '{code}' has no cases to activate.");

    public static Error OpenDefects(string code, int count) =>
        Error.Conflict("TESTPLAN.OPEN_DEFECTS", $"Test plan '{code}' still has {count} failed case(s) without a passing retest.");

    public static Error DefectRequired(string reference) =>
        Error.Validation("TESTPLAN.DEFECT_REQUIRED", $"Test case '{reference}' failed, so a defect reference is required.");

    public static readonly Error NotAnExecution =
        Error.Validation("TESTPLAN.NOT_AN_EXECUTION", "An execution must be recorded as passed, failed or blocked.");
}
