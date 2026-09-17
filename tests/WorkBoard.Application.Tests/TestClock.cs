using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.Tests;

/// <summary>
/// A clock that does not move unless a test moves it.
/// </summary>
/// <remarks>
/// The whole reason <see cref="IClock"/> exists. Deadlines, token expiry and cycle time are business
/// rules that depend on "now", and a test that asserted on them against the system clock would
/// either sleep or be flaky at midnight.
/// </remarks>
internal sealed class TestClock(DateTimeOffset now) : IClock
{
    /// <summary>Starts at a fixed, arbitrary instant in UTC.</summary>
    public TestClock()
        : this(new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero))
    {
    }

    /// <inheritdoc/>
    public DateTimeOffset UtcNow { get; private set; } = now;

    /// <summary>Moves the clock forward.</summary>
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
