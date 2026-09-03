using System.Runtime.CompilerServices;

namespace PDR.BuildingBlocks.Core.Guards;

public static class Ensure
{
    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value must not be empty.", name);
        }

        return value;
    }

    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(value, name);
        return value;
    }

    public static string MaxLength(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Value exceeds the maximum length of {maxLength}.", name);
        }

        return value;
    }
}
