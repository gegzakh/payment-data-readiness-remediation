using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Parsing;

/// <summary>One party address as produced by a parser, before it is persisted.</summary>
public sealed record ParsedAddress(
    string? MessageId,
    string? EndToEndId,
    PartyRole PartyRole,
    string? PartyName,
    string? Country,
    string? TownName,
    string? PostCode,
    string? StreetName,
    string? BuildingNumber,
    string? AddressLines);

/// <summary>A record the parser could not turn into an address, kept as a non-sensitive reason (FR-ING-004).</summary>
public sealed record ParseFailure(int Sequence, string Reason);

public sealed record ParseOutcome(
    IReadOnlyList<ParsedAddress> Addresses,
    IReadOnlyList<ParseFailure> Failures,
    int InputRecordCount,
    int ExcludedCount);

public interface IAddressParser
{
    IngestionFormat Format { get; }

    string Version { get; }

    /// <summary>Parses the payload. Structural problems throw <see cref="ParserException"/>; per-record problems are returned as failures.</summary>
    ParseOutcome Parse(Stream payload, ParserOptions options);
}

public sealed record ParserOptions(string Delimiter, int MaxRecords);

public sealed class ParserException(string message) : Exception(message);
