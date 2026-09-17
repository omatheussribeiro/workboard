using WorkBoard.Application.Interfaces;

namespace WorkBoard.Infrastructure.Services;

/// <summary>
/// The production clock.
/// </summary>
/// <remarks>
/// Wraps <see cref="TimeProvider"/> rather than calling <c>DateTimeOffset.UtcNow</c> directly, so
/// tests can substitute a <c>FakeTimeProvider</c> at the framework level as well as through
/// <see cref="IClock"/>.
/// </remarks>
public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
