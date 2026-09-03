using PDR.BuildingBlocks.Domain;

namespace PDR.Rules.Domain.Rulesets;

public sealed record RulesetVersionActivated(
    Guid RulesetId,
    string SchemeCode,
    int VersionNumber,
    DateOnly EffectiveFrom,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
