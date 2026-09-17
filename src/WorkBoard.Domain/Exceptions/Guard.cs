using System.Runtime.CompilerServices;

namespace WorkBoard.Domain.Exceptions;

/// <summary>
/// Guard clauses for invariants that must hold on every construction path.
/// These raise <see cref="DomainException"/> rather than ArgumentException: a violation here is a
/// broken business rule, not a programming error, and the API surfaces it as a 422 with a message.
/// </summary>
internal static class Guard
{
    public static string NotEmpty(string? value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{name} is required.");
        }

        return value.Trim();
    }

    public static string MaxLength(string value, int max, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value.Length > max)
        {
            throw new DomainException($"{name} must be at most {max} characters.");
        }

        return value;
    }

    public static Guid NotDefault(Guid value, [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} is required.");
        }

        return value;
    }

    public static void Against(bool condition, string message)
    {
        if (condition)
        {
            throw new DomainException(message);
        }
    }
}
