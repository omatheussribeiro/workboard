using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Comments.ListComments;

/// <inheritdoc cref="IListCommentsUseCase"/>
public sealed class ListCommentsUseCase(ICommentQueries queries, ICurrentUser currentUser) : IListCommentsUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<CommentDto>> ExecuteAsync(
        Guid taskId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await queries.ListAsync(taskId, currentUser.RequireUserId(), page, cancellationToken);
}
