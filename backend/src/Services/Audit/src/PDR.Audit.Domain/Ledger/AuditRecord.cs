using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PDR.BuildingBlocks.Core.Guards;
using PDR.BuildingBlocks.Domain;

namespace PDR.Audit.Domain.Ledger;

public enum AuditOutcome
{
    Success = 0,
    Failure = 1,
    Denied = 2
}

/// <summary>
/// One append-only entry of the evidential audit ledger. Each record hashes its own content together
/// with the previous record's hash, so any later edit or deletion of history is detectable
/// (FR-AUD-002): the chain no longer recomputes.
/// </summary>
public sealed class AuditRecord : Entity
{
    /// <summary>Hash the first record of the chain links to.</summary>
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    private AuditRecord()
    {
    }

    private AuditRecord(
        long sequence,
        DateTimeOffset occurredAtUtc,
        string service,
        string action,
        string entityType,
        string entityId,
        string actor,
        string? actorId,
        AuditOutcome outcome,
        string? correlationId,
        string? legalEntity,
        string? details,
        string previousHash)
    {
        Sequence = sequence;
        OccurredAtUtc = Normalize(occurredAtUtc);
        Service = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(service), 64);
        Action = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(action), 128);
        EntityType = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(entityType), 128);
        EntityId = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(entityId), 128);
        Actor = Ensure.MaxLength(Ensure.NotNullOrWhiteSpace(actor), 256);
        ActorId = actorId;
        Outcome = outcome;
        CorrelationId = correlationId;
        LegalEntity = legalEntity;
        Details = details;
        PreviousHash = previousHash;
        Hash = ComputeHash();
    }

    /// <summary>Position in the chain; gaps or reordering invalidate verification.</summary>
    public long Sequence { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string Service { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string Actor { get; private set; } = string.Empty;

    public string? ActorId { get; private set; }

    public AuditOutcome Outcome { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? LegalEntity { get; private set; }

    /// <summary>Free-form JSON payload describing before/after values or the reason for a denial.</summary>
    public string? Details { get; private set; }

    public string PreviousHash { get; private set; } = GenesisHash;

    public string Hash { get; private set; } = string.Empty;

    public static AuditRecord Append(
        AuditRecord? previous,
        DateTimeOffset occurredAtUtc,
        string service,
        string action,
        string entityType,
        string entityId,
        string actor,
        string? actorId = null,
        AuditOutcome outcome = AuditOutcome.Success,
        string? correlationId = null,
        string? legalEntity = null,
        string? details = null) =>
        new(
            (previous?.Sequence ?? 0) + 1,
            occurredAtUtc,
            service,
            action,
            entityType,
            entityId,
            actor,
            actorId,
            outcome,
            correlationId,
            legalEntity,
            details,
            previous?.Hash ?? GenesisHash);

    /// <summary>True when the stored hash still matches the record's content and the link to <paramref name="previous"/>.</summary>
    public bool IsIntact(AuditRecord? previous) =>
        PreviousHash == (previous?.Hash ?? GenesisHash)
        && Sequence == (previous?.Sequence ?? 0) + 1
        && Hash == ComputeHash();

    /// <summary>
    /// PostgreSQL stores timestamps with microsecond precision, so the ticks a record is created with must be
    /// truncated up front; otherwise a round-tripped record hashes differently from the one that was appended.
    /// </summary>
    private static DateTimeOffset Normalize(DateTimeOffset value)
    {
        var utc = value.ToUniversalTime();
        return utc.AddTicks(-(utc.Ticks % (TimeSpan.TicksPerMillisecond / 1000)));
    }

    private string ComputeHash()
    {
        var canonical = string.Join(
            '|',
            Sequence.ToString(CultureInfo.InvariantCulture),
            OccurredAtUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ", CultureInfo.InvariantCulture),
            Service,
            Action,
            EntityType,
            EntityId,
            Actor,
            ActorId ?? string.Empty,
            Outcome.ToString(),
            CorrelationId ?? string.Empty,
            LegalEntity ?? string.Empty,
            Details ?? string.Empty,
            PreviousHash);

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
