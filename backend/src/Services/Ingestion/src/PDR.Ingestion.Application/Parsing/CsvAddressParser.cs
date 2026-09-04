using System.Text;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Parsing;

/// <summary>
/// Parses the approved delimited layout (FR-ING-002). The header drives the mapping, so column order
/// is free and unknown columns are ignored; a row missing every address column is excluded rather
/// than failed, because it carries nothing to assess.
/// </summary>
public sealed class CsvAddressParser : IAddressParser
{
    private static readonly string[] RequiredColumns = ["partyrole"];

    public IngestionFormat Format => IngestionFormat.Csv;

    public string Version => "csv-1.0";

    public ParseOutcome Parse(Stream payload, ParserOptions options)
    {
        using var reader = new StreamReader(payload, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var headerLine = reader.ReadLine()
                         ?? throw new ParserException("The file is empty; a header row is required.");

        var header = Split(headerLine, options.Delimiter)
            .Select((column, index) => (Column: Normalize(column), Index: index))
            .ToDictionary(entry => entry.Column, entry => entry.Index, StringComparer.Ordinal);

        var missing = RequiredColumns.Where(column => !header.ContainsKey(column)).ToList();
        if (missing.Count > 0)
        {
            throw new ParserException($"The header is missing the required column(s): {string.Join(", ", missing)}.");
        }

        var addresses = new List<ParsedAddress>();
        var failures = new List<ParseFailure>();
        var excluded = 0;
        var sequence = 0;

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            sequence++;
            if (sequence > options.MaxRecords)
            {
                throw new ParserException($"The file exceeds the configured maximum of {options.MaxRecords} records.");
            }

            var fields = Split(line, options.Delimiter);

            var roleText = Value(fields, header, "partyrole");
            if (!Enum.TryParse<PartyRole>(roleText, ignoreCase: true, out var role))
            {
                failures.Add(new ParseFailure(sequence, $"Unknown party role '{roleText}'."));
                continue;
            }

            var address = new ParsedAddress(
                Value(fields, header, "messageid"),
                Value(fields, header, "endtoendid"),
                role,
                Value(fields, header, "partyname"),
                Value(fields, header, "country"),
                Value(fields, header, "town"),
                Value(fields, header, "postcode"),
                Value(fields, header, "street"),
                Value(fields, header, "buildingnumber"),
                Value(fields, header, "addresslines"));

            if (CarriesNothing(address))
            {
                excluded++;
                continue;
            }

            addresses.Add(address);
        }

        return new ParseOutcome(addresses, failures, sequence, excluded);
    }

    private static bool CarriesNothing(ParsedAddress address) =>
        string.IsNullOrWhiteSpace(address.Country) &&
        string.IsNullOrWhiteSpace(address.TownName) &&
        string.IsNullOrWhiteSpace(address.PostCode) &&
        string.IsNullOrWhiteSpace(address.StreetName) &&
        string.IsNullOrWhiteSpace(address.BuildingNumber) &&
        string.IsNullOrWhiteSpace(address.AddressLines) &&
        string.IsNullOrWhiteSpace(address.PartyName);

    private static string? Value(List<string> fields, Dictionary<string, int> header, string column)
    {
        if (!header.TryGetValue(column, out var index) || index >= fields.Count)
        {
            return null;
        }

        var value = fields[index].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Normalize(string column) =>
        new(column.Trim().Trim('"').ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>Minimal RFC 4180 handling: quoted fields may contain the delimiter and doubled quotes.</summary>
    private static List<string> Split(string line, string delimiter)
    {
        var separator = delimiter.Length > 0 ? delimiter[0] : ',';
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == separator && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
