using System.Text.RegularExpressions;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Rulesets;

/// <summary>
/// Aggregate root owning the versioned rules of one payment scheme. Draft versions are editable,
/// activation is dated and retires the previously active version, and an older version can be
/// re-activated as a rollback (FR-RUL-003/FR-RUL-004).
/// </summary>
public sealed class Ruleset : AggregateRoot
{
    private readonly List<RulesetVersion> _versions = [];

    private Ruleset()
    {
    }

    private Ruleset(string schemeCode, string name, string? description)
    {
        SchemeCode = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(schemeCode), 32).ToUpperInvariant();
        Name = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(name), 128);
        Description = description;
        _versions.Add(new RulesetVersion(Id, 1, "Initial draft"));
    }

    public string SchemeCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyList<RulesetVersion> Versions => _versions;

    public RulesetVersion? ActiveVersion => _versions.Find(version => version.Status == RulesetStatus.Active);

    public static Ruleset Create(string schemeCode, string name, string? description) =>
        new(schemeCode, name, description);

    public Result<RulesetVersion> AddVersion(int? copyFromVersionNumber, string? notes)
    {
        RulesetVersion? source = null;
        if (copyFromVersionNumber is not null)
        {
            source = _versions.Find(version => version.VersionNumber == copyFromVersionNumber);
            if (source is null)
            {
                return Result.Failure<RulesetVersion>(RulesetErrors.VersionNotFound(copyFromVersionNumber.Value));
            }
        }

        var version = new RulesetVersion(Id, _versions.Max(v => v.VersionNumber) + 1, notes);
        if (source is not null)
        {
            version.CopyRulesFrom(source);
        }

        _versions.Add(version);
        return version;
    }

    public Result<RuleDefinition> AddRule(
        int versionNumber,
        string code,
        string field,
        RuleKind kind,
        RuleSeverity severity,
        RuleApplicability applicability,
        string message,
        string? parameter)
    {
        var version = Find(versionNumber);
        if (version is null)
        {
            return Result.Failure<RuleDefinition>(RulesetErrors.VersionNotFound(versionNumber));
        }

        if (version.Status != RulesetStatus.Draft)
        {
            return Result.Failure<RuleDefinition>(RulesetErrors.VersionIsImmutable);
        }

        if (version.HasRuleCode(code))
        {
            return Result.Failure<RuleDefinition>(RulesetErrors.DuplicateRuleCode(code));
        }

        var parameterCheck = ValidateParameter(kind, parameter);
        if (parameterCheck.IsFailure)
        {
            return Result.Failure<RuleDefinition>(parameterCheck.Error);
        }

        return version.AddRule(code, field, kind, severity, applicability, message, parameter);
    }

    public Result RemoveRule(int versionNumber, Guid ruleId)
    {
        var version = Find(versionNumber);
        if (version is null)
        {
            return Result.Failure(RulesetErrors.VersionNotFound(versionNumber));
        }

        if (version.Status != RulesetStatus.Draft)
        {
            return Result.Failure(RulesetErrors.VersionIsImmutable);
        }

        return version.RemoveRule(ruleId)
            ? Result.Success()
            : Result.Failure(RulesetErrors.RuleNotFound(ruleId));
    }

    /// <summary>
    /// Makes a version the one Validation evaluates from <paramref name="effectiveFrom"/>. Re-activating an
    /// already retired version is the rollback path, so a bad ruleset can be undone without editing history.
    /// </summary>
    public Result Activate(int versionNumber, DateOnly effectiveFrom, string actor, DateTimeOffset activatedAtUtc)
    {
        var version = Find(versionNumber);
        if (version is null)
        {
            return Result.Failure(RulesetErrors.VersionNotFound(versionNumber));
        }

        if (version.Status == RulesetStatus.Active)
        {
            return Result.Failure(RulesetErrors.VersionAlreadyActive);
        }

        if (version.Rules.Count == 0)
        {
            return Result.Failure(RulesetErrors.NoRules);
        }

        ActiveVersion?.Retire(effectiveFrom);
        version.Activate(effectiveFrom, actor, activatedAtUtc);

        Raise(new RulesetVersionActivated(Id, SchemeCode, version.VersionNumber, effectiveFrom, activatedAtUtc));
        return Result.Success();
    }

    private RulesetVersion? Find(int versionNumber) =>
        _versions.Find(version => version.VersionNumber == versionNumber);

    private static Result ValidateParameter(RuleKind kind, string? parameter) => kind switch
    {
        RuleKind.MaxLength when !int.TryParse(parameter, out var length) || length <= 0 =>
            Result.Failure(RulesetErrors.InvalidParameter(kind, "a positive integer")),
        RuleKind.Pattern when !IsCompilableRegex(parameter) =>
            Result.Failure(RulesetErrors.InvalidParameter(kind, "a valid regular expression")),
        RuleKind.AllowedValues when string.IsNullOrWhiteSpace(parameter) =>
            Result.Failure(RulesetErrors.InvalidParameter(kind, "a comma separated value list")),
        _ => Result.Success()
    };

    private static bool IsCompilableRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            _ = Regex.Match(string.Empty, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
