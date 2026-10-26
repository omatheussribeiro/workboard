using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Comments.GetComment;

/// <inheritdoc cref="IGetCommentUseCase"/>
public sealed class GetCommentUseCase(ICommentQueries queries, ICurrentUser currentUser) : IGetCommentUseCase
{
    /// <inheritdoc/>
    public async Task<CommentDto> ExecuteAsync(Guid commentId, CancellationToken cancellationToken) =>
        await queries.GetAsync(commentId, currentUser.RequireUserId(), cancellationToken)
        ?? throw new NotFoundException("Comment", commentId);
}
