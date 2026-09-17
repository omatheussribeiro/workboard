
namespace WorkBoard.Domain.Interfaces;

/// <summary>
/// Timestamps stamped centrally in <c>SaveChangesAsync</c> so no handler has to remember.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; }

    DateTimeOffset? UpdatedAtUtc { get; }

    void SetCreated(DateTimeOffset at);

    void SetUpdated(DateTimeOffset at);
}
