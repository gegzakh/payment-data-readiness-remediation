using System.Security.Cryptography;
using System.Text;
using PDR.BuildingBlocks.Core.Settings;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest;

public sealed record FileSafetyOptions(long MaxFileBytes, IReadOnlyList<string> AllowedExtensions, int MaxRecords, string CsvDelimiter);

public sealed record FileInspection(bool IsSafe, string? RejectionReason, string Checksum);

/// <summary>
/// Pre-processing gate for every payload: extension, size, encoding, emptiness and a malware signature
/// check, plus the checksum used for duplicate detection (FR-ING-003, FR-ING-007). Rejection reasons are
/// deliberately non-sensitive — they never echo file content (FR-ING-004).
/// </summary>
public sealed class FileSafetyInspector(ISettingsReader settings)
{
    public const long DefaultMaxFileBytes = 25 * 1024 * 1024;
    public const int DefaultMaxRecords = 100_000;
    public const string DefaultAllowedExtensions = ".xml,.csv,.txt";
    public const string DefaultCsvDelimiter = ",";

    /// <summary>The EICAR test string; a real deployment delegates to the enterprise scanner.</summary>
    private const string MalwareSignature = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR";

    public async Task<FileSafetyOptions> ResolveOptionsAsync(CancellationToken cancellationToken = default)
    {
        var maxBytes = await settings.GetAsync(IngestionSettingKeys.MaxFileBytes, DefaultMaxFileBytes, cancellationToken);
        var maxRecords = await settings.GetAsync(IngestionSettingKeys.MaxRecords, DefaultMaxRecords, cancellationToken);
        var extensions = await settings.GetAsync(IngestionSettingKeys.AllowedExtensions, DefaultAllowedExtensions, cancellationToken);
        var delimiter = await settings.GetAsync(IngestionSettingKeys.CsvDelimiter, DefaultCsvDelimiter, cancellationToken);

        return new FileSafetyOptions(
            maxBytes,
            extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            maxRecords,
            string.IsNullOrEmpty(delimiter) ? DefaultCsvDelimiter : delimiter);
    }

    public static FileInspection Inspect(string fileName, IngestionFormat format, byte[] content, FileSafetyOptions options)
    {
        var checksum = Convert.ToHexStringLower(SHA256.HashData(content));

        var reason = FindRejection(fileName, format, content, options);
        return new FileInspection(reason is null, reason, checksum);
    }

    private static string? FindRejection(string fileName, IngestionFormat format, byte[] content, FileSafetyOptions options)
    {
        if (content.Length == 0)
        {
            return "The uploaded file is empty.";
        }

        if (content.Length > options.MaxFileBytes)
        {
            return $"The file exceeds the maximum accepted size of {options.MaxFileBytes} bytes.";
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return $"File type '{extension}' is not accepted. Allowed types: {string.Join(", ", options.AllowedExtensions)}.";
        }

        if (!TryDecodeUtf8(content, out var text))
        {
            return "The file is not valid UTF-8 text.";
        }

        if (text.Contains(MalwareSignature, StringComparison.Ordinal))
        {
            return "The file was rejected by the malware scanner.";
        }

        if (format == IngestionFormat.Iso20022Xml && !text.TrimStart().StartsWith('<'))
        {
            return "The file does not contain an XML document.";
        }

        if (text.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("<!ENTITY", StringComparison.OrdinalIgnoreCase))
        {
            return "Document type definitions and entity declarations are not accepted.";
        }

        return null;
    }

    private static bool TryDecodeUtf8(byte[] content, out string text)
    {
        try
        {
            text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(content);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }
}
