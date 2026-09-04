using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDR.BuildingBlocks.Domain;

namespace PDR.BuildingBlocks.Persistence.Settings;

/// <summary>
/// Runtime-configurable setting. Precedence is DB setting → environment variable → appsettings.json,
/// so operations can change behaviour (page sizes, thresholds, SLAs) without a redeploy.
/// </summary>
public sealed class SystemSetting : AggregateRoot
{
    private SystemSetting()
    {
    }

    public SystemSetting(string key, string value, string valueType, string? description, bool isSensitive = false)
    {
        Key = key;
        Value = value;
        ValueType = valueType;
        Description = description;
        IsSensitive = isSensitive;
    }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public string ValueType { get; private set; } = "string";

    public string? Description { get; private set; }

    public bool IsSensitive { get; private set; }

    public void Update(string value) => Value = value;
}

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ValueType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ModifiedBy).HasMaxLength(256);
        builder.UseRowVersionConcurrencyToken();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
