
namespace WorkBoard.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would leave an aggregate in a state the domain forbids.
/// Translated to HTTP 422 by the API's exception handler.
/// </summary>
public sealed class DomainException(string message) : Exception(message)
{
    public static void Throw(string message) => throw new DomainException(message);
}
