using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Simulation.Domain.Scenarios;

/// <summary>
/// A named, reproducible question about the portfolio: which population, evaluated against which rules,
/// as of which date (FR-SIM-001). Locking a scenario freezes its definition so two runs of the same
/// scenario are comparable (FR-SIM-002).
/// </summary>
public sealed class Scenario : AggregateRoot
{
    private Scenario()
    {
    }

    private Scenario(
        string code,
        string name,
        ScenarioMode mode,
        DateOnly asOf,
        string? schemeCodes,
        string? sourceCodes,
        string? countries,
        string? partyRoles,
        string? exclusions,
        string? rulesetVersion,
        string? description)
    {
        Code = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(code), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        Mode = mode;
        AsOf = asOf;
        SchemeCodes = Normalize(schemeCodes);
        SourceCodes = Normalize(sourceCodes);
        Countries = Normalize(countries);
        PartyRoles = Normalize(partyRoles);
        Exclusions = exclusions is null ? null : Ensure.MaxLength(exclusions, 512);
        RulesetVersion = rulesetVersion is null ? null : Ensure.MaxLength(rulesetVersion, 32);
        Description = description is null ? null : Ensure.MaxLength(description, 1024);
        Status = ScenarioStatus.Draft;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ScenarioMode Mode { get; private set; }

    /// <summary>The date the rules are evaluated at; for a future scenario this is the cutover date.</summary>
    public DateOnly AsOf { get; private set; }

    public string? SchemeCodes { get; private set; }

    public string? SourceCodes { get; private set; }

    public string? Countries { get; private set; }

    public string? PartyRoles { get; private set; }

    /// <summary>Populations deliberately left out, recorded so a result can never be read as complete.</summary>
    public string? Exclusions { get; private set; }

    public string? RulesetVersion { get; private set; }

    public string? Description { get; private set; }

    public ScenarioStatus Status { get; private set; }

    public static Scenario Create(
        string code,
        string name,
        ScenarioMode mode,
        DateOnly asOf,
        string? schemeCodes = null,
        string? sourceCodes = null,
        string? countries = null,
        string? partyRoles = null,
        string? exclusions = null,
        string? rulesetVersion = null,
        string? description = null) =>
        new(code, name, mode, asOf, schemeCodes, sourceCodes, countries, partyRoles, exclusions, rulesetVersion, description);

    public Result Update(
        string name,
        DateOnly asOf,
        string? schemeCodes,
        string? sourceCodes,
        string? countries,
        string? partyRoles,
        string? exclusions,
        string? rulesetVersion,
        string? description)
    {
        if (Status != ScenarioStatus.Draft)
        {
            return Result.Failure(ScenarioErrors.NotDraft(Code, Status));
        }

        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 140);
        AsOf = asOf;
        SchemeCodes = Normalize(schemeCodes);
        SourceCodes = Normalize(sourceCodes);
        Countries = Normalize(countries);
        PartyRoles = Normalize(partyRoles);
        Exclusions = exclusions is null ? null : Ensure.MaxLength(exclusions, 512);
        RulesetVersion = rulesetVersion is null ? null : Ensure.MaxLength(rulesetVersion, 32);
        Description = description is null ? null : Ensure.MaxLength(description, 1024);
        return Result.Success();
    }

    public Result Lock()
    {
        if (Status == ScenarioStatus.Archived)
        {
            return Result.Failure(ScenarioErrors.Archived(Code));
        }

        Status = ScenarioStatus.Locked;
        return Result.Success();
    }

    public Result Archive()
    {
        Status = ScenarioStatus.Archived;
        return Result.Success();
    }

    /// <summary>True when the scenario can still be executed; an archived definition cannot.</summary>
    public bool IsRunnable => Status != ScenarioStatus.Archived;

    private static string? Normalize(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? null
            : string.Join(
                ',',
                csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => value.ToUpperInvariant())
                    .Distinct()
                    .Order());
}

public static class ScenarioErrors
{
    public static Error NotFound(string code) =>
        Error.NotFound("SCENARIO.NOT_FOUND", $"Scenario '{code}' was not found.");

    public static Error Duplicate(string code) =>
        Error.Conflict("SCENARIO.DUPLICATE", $"Scenario '{code}' already exists.");

    public static Error NotDraft(string code, ScenarioStatus status) =>
        Error.Conflict("SCENARIO.NOT_DRAFT", $"Scenario '{code}' is {status} and can no longer be edited.");

    public static Error Archived(string code) =>
        Error.Conflict("SCENARIO.ARCHIVED", $"Scenario '{code}' is archived.");
}
