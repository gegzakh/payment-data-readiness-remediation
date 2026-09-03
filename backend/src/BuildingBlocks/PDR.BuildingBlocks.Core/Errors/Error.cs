namespace PDR.BuildingBlocks.Core.Errors;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Concurrency = 4,
    Unauthorized = 5,
    Forbidden = 6,
    Unprocessable = 7,
    RateLimited = 8,
    Dependency = 9
}

/// <summary>
/// An expected, domain-meaningful failure. Infrastructure faults throw; everything else is an <see cref="Error"/>.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Shared instance so two errors without validation details compare equal.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors =
        new Dictionary<string, string[]>();

    public static readonly Error None = new(string.Empty, string.Empty);

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = NoValidationErrors;

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]> errors) =>
        new(code, message, ErrorType.Validation) { ValidationErrors = errors };

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Concurrency(string code, string message) => new(code, message, ErrorType.Concurrency);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Unprocessable(string code, string message) => new(code, message, ErrorType.Unprocessable);

    public static Error Dependency(string code, string message) => new(code, message, ErrorType.Dependency);
}
