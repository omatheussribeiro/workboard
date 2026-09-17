using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to task comments.</summary>
public interface ICommentRepository
{
    /// <summary>Loads a comment tracked.</summary>
    Task<Comment?> FindForUpdateAsync(Guid commentId, CancellationToken cancellationToken);
}
