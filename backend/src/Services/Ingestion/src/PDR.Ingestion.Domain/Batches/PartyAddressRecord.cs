using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PDR.BuildingBlocks.Domain;

namespace PDR.Ingestion.Domain.Batches;

/// <summary>
/// One party address extracted from an ingested message or row (FR-VAL-001). Only the fields needed
/// to assess address readiness are kept; the rest of the payment is deliberately not persisted.
/// </summary>
public sealed class PartyAddressRecord : Entity
{
    private PartyAddressRecord()
    {
    }

#pragma warning disable S107 // An address record genuinely carries this many independent fields.
    private PartyAddressRecord(
        Guid batchId,
        int sequence,
        string? messageId,
        string? endToEndId,
        PartyRole partyRole,
        string? partyName,
        string? country,
        string? townName,
        string? postCode,
        string? streetName,
        string? buildingNumber,
        string? addressLines,
        string? schemeCode)
    {
        BatchId = batchId;
        Sequence = sequence;
        MessageId = Truncate(messageId, 64);
        EndToEndId = Truncate(endToEndId, 64);
        PartyRole = partyRole;
        PartyName = Truncate(partyName, 140);
        Country = Truncate(country?.Trim().ToUpperInvariant(), 8);
        TownName = Truncate(townName, 140);
        PostCode = Truncate(postCode, 32);
        StreetName = Truncate(streetName, 140);
        BuildingNumber = Truncate(buildingNumber, 32);
        AddressLines = Truncate(addressLines, 1024);
        SchemeCode = Truncate(schemeCode?.ToUpperInvariant(), 32);
        ContentHash = ComputeHash();
    }
#pragma warning restore S107

    public Guid BatchId { get; private set; }

    public int Sequence { get; private set; }

    public string? MessageId { get; private set; }

    public string? EndToEndId { get; private set; }

    public PartyRole PartyRole { get; private set; }

    public string? PartyName { get; private set; }

    public string? Country { get; private set; }

    public string? TownName { get; private set; }

    public string? PostCode { get; private set; }

    public string? StreetName { get; private set; }

    public string? BuildingNumber { get; private set; }

    /// <summary>Unstructured address lines, newline separated, as they appeared in the source.</summary>
    public string? AddressLines { get; private set; }

    public string? SchemeCode { get; private set; }

    /// <summary>Stable hash of the address content, used to detect duplicates within a batch (FR-VAL-008).</summary>
    public string ContentHash { get; private set; } = string.Empty;

    public bool IsDuplicate { get; private set; }

#pragma warning disable S107
    public static PartyAddressRecord Create(
        Guid batchId,
        int sequence,
        string? messageId,
        string? endToEndId,
        PartyRole partyRole,
        string? partyName,
        string? country,
        string? townName,
        string? postCode,
        string? streetName,
        string? buildingNumber,
        string? addressLines,
        string? schemeCode) =>
        new(
            batchId,
            sequence,
            messageId,
            endToEndId,
            partyRole,
            partyName,
            country,
            townName,
            postCode,
            streetName,
            buildingNumber,
            addressLines,
            schemeCode);
#pragma warning restore S107

    public void MarkDuplicate() => IsDuplicate = true;

    private string ComputeHash()
    {
        var canonical = string.Join(
            '|',
            PartyRole.ToString(),
            Normalize(PartyName),
            Normalize(Country),
            Normalize(TownName),
            Normalize(PostCode),
            Normalize(StreetName),
            Normalize(BuildingNumber),
            Normalize(AddressLines));

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.ToUpper(CultureInfo.InvariantCulture)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
