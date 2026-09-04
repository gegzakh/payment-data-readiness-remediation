using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Domain;

namespace PDR.Remediation.Infrastructure.WriteBack;

/// <summary>
/// The stand-in for a source system's own store. A real deployment writes through that system's API;
/// locally the connector writes here so read-after-write, staleness and rollback behave for real.
/// </summary>
public sealed class SimulatedSourceRecord : Entity
{
    private SimulatedSourceRecord()
    {
    }

    public SimulatedSourceRecord(string sourceCode, string recordReference, string value)
    {
        SourceCode = sourceCode.ToUpperInvariant();
        RecordReference = recordReference;
        Value = value;
        Version = "1";
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string SourceCode { get; private set; } = string.Empty;

    public string RecordReference { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public string Version { get; private set; } = "1";

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Write(string value, DateTimeOffset atUtc)
    {
        Value = value;
        Version = (int.TryParse(Version, out var version) ? version + 1 : 1).ToString();
        UpdatedAtUtc = atUtc;
    }
}

public sealed class SimulatedSourceRecordConfiguration : IEntityTypeConfiguration<SimulatedSourceRecord>
{
    public void Configure(EntityTypeBuilder<SimulatedSourceRecord> builder)
    {
        builder.ToTable("simulated_source_records");
        builder.HasKey(record => record.Id);

        builder.Property(record => record.SourceCode).HasMaxLength(32).IsRequired();
        builder.Property(record => record.RecordReference).HasMaxLength(256).IsRequired();
        builder.Property(record => record.Value).HasMaxLength(1024).IsRequired();
        builder.Property(record => record.Version).HasMaxLength(32).IsRequired();

        builder.HasIndex(record => new { record.SourceCode, record.RecordReference }).IsUnique();
    }
}
