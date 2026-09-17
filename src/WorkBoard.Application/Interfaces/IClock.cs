namespace WorkBoard.Application.Interfaces;

/// <summary>
/// Injected time.
/// </summary>
/// <remarks>
/// Deadlines, token expiry and cycle time are all business rules that depend on "now". Reading the
/// system clock inside them would make those rules testable only by sleeping. The production
/// implementation wraps <see cref="TimeProvider"/>; tests supply a fake.
/// </remarks>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
