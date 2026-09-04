namespace PDR.Simulation.Domain.Scenarios;

/// <summary>Which world a scenario evaluates: the rules in force today, the post-cutover rules, or the
/// post-cutover rules with every approved remediation already applied (FR-SIM-001).</summary>
public enum ScenarioMode
{
    Current = 0,
    Future = 1,
    Remediated = 2
}

public enum ScenarioStatus
{
    Draft = 0,

    /// <summary>Locked scenarios are immutable so a run stays reproducible (FR-SIM-002).</summary>
    Locked = 1,
    Archived = 2
}

public enum RunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2
}

/// <summary>How a breakdown row was cut, so a stored result can be re-read without the source data.</summary>
public enum BreakdownDimension
{
    Scheme = 0,
    Source = 1,
    Country = 2,
    PartyRole = 3,
    Issue = 4
}
