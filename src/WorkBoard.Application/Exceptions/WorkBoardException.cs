namespace WorkBoard.Application.Exceptions;

/// <summary>
/// Base type for the handful of failure shapes the application layer signals to the transport.
/// </summary>
/// <remarks>
/// <para>
/// Exceptions rather than a <c>Result&lt;T&gt;</c> type. The trade-off was made knowingly: a result
/// type makes failure visible in the signature, but in a service that performs six lookups it turns
/// every one into a match-and-propagate ladder, and the failure here is genuinely exceptional —
/// nothing between the service and the endpoint wants to inspect it. One exception handler maps
/// these to RFC 9457 ProblemDetails, so the mapping lives in exactly one place. See ADR-009.
/// </para>
/// <para>
/// Named <c>WorkBoardException</c> rather than <c>ApplicationException</c> so it does not shadow
/// <see cref="System.ApplicationException"/>. A type that hides a framework type of the same name
/// makes every <c>catch</c> clause ambiguous to the reader, and which one is caught then depends on
/// the <c>using</c> directives at the top of the file.
/// </para>
/// </remarks>
public abstract class WorkBoardException(string message) : Exception(message);
