using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="ICommentRepository"/>
public sealed class CommentRepository(WorkBoardDbContext context) : ICommentRepository
{
    /// <inheritdoc/>
    /// <remarks>
    /// Deleted comments are still returned. The rules about who may edit or delete live on the
    /// entity, and hiding an already-deleted row here would turn a second delete into a 404 rather
    /// than the no-op the entity makes it.
    /// </remarks>
    public Task<Comment?> FindForUpdateAsync(Guid commentId, CancellationToken cancellationToken) =>
        context.Comments.FirstOrDefaultAsync(comment => comment.Id == commentId, cancellationToken);
}
