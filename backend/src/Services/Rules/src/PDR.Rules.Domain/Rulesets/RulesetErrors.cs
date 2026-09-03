using PDR.BuildingBlocks.Core.Errors;

namespace PDR.Rules.Domain.Rulesets;

public static class RulesetErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RULESET.NOT_FOUND", $"Ruleset '{id}' was not found.");

    public static Error VersionNotFound(int versionNumber) =>
        Error.NotFound("RULESET.VERSION_NOT_FOUND", $"Ruleset version '{versionNumber}' was not found.");

    public static Error RuleNotFound(Guid id) =>
        Error.NotFound("RULESET.RULE_NOT_FOUND", $"Rule '{id}' was not found.");

    public static Error DuplicateRuleCode(string code) =>
        Error.Conflict("RULESET.DUPLICATE_RULE_CODE", $"Rule code '{code}' is already used in this version.");

    public static Error InvalidParameter(RuleKind kind, string expected) =>
        Error.Validation("RULESET.INVALID_PARAMETER", $"A '{kind}' rule requires {expected} as its parameter.");

    public static readonly Error VersionIsImmutable =
        Error.Conflict("RULESET.VERSION_IMMUTABLE", "Only draft ruleset versions can be edited; create a new version.");

    public static readonly Error VersionAlreadyActive =
        Error.Conflict("RULESET.VERSION_ALREADY_ACTIVE", "The ruleset version is already active.");

    public static readonly Error NoRules =
        Error.Unprocessable("RULESET.NO_RULES", "A ruleset version must contain at least one rule before activation.");

    public static readonly Error SchemeAlreadyExists =
        Error.Conflict("SCHEME.ALREADY_EXISTS", "A scheme with this code already exists.");

    public static readonly Error RulesetAlreadyExists =
        Error.Conflict("RULESET.ALREADY_EXISTS", "A ruleset already exists for this scheme.");

    public static Error SchemeNotFound(string code) =>
        Error.NotFound("SCHEME.NOT_FOUND", $"Scheme '{code}' was not found.");

    public static Error NoActiveRuleset(string schemeCode, DateOnly asOf) =>
        Error.NotFound(
            "RULESET.NO_EFFECTIVE_VERSION",
            $"No ruleset version is effective for scheme '{schemeCode}' on {asOf:yyyy-MM-dd}.");
}
