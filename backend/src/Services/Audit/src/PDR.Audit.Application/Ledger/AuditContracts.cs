using PDR.Audit.Domain.Ledger;

namespace PDR.Audit.Application.Ledger;

public sealed record AuditRecordDto(
    Guid Id,
    long Sequence,
    DateTimeOffset OccurredAtUtc,
    string Service,
    string Action,
    string EntityType,
    string EntityId,
    string Actor,
    string? ActorId,
    AuditOutcome Outcome,
    string? CorrelationId,
    string? LegalEntity,
    string? Details,
    string PreviousHash,
    string Hash);

/// <summary>
/// Outcome of re-hashing the ledger. <paramref name="FirstBrokenSequence"/> pinpoints where the chain
/// stops reproducing, which is the evidence auditors ask for.
/// </summary>
public sealed record AuditChainVerificationDto(
    bool IsIntact,
    long RecordsChecked,
    long? FirstBrokenSequence,
    DateTimeOffset VerifiedAtUtc);

public static class AuditMapping
{
    public static AuditRecordDto ToDto(this AuditRecord record) =>
        new(
            record.Id,
            record.Sequence,
            record.OccurredAtUtc,
            record.Service,
            record.Action,
            record.EntityType,
            record.EntityId,
            record.Actor,
            record.ActorId,
            record.Outcome,
            record.CorrelationId,
            record.LegalEntity,
            record.Details,
            record.PreviousHash,
            record.Hash);
}
