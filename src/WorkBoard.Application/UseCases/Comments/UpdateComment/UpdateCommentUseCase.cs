using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Comments.UpdateComment;

/// <inheritdoc cref="IUpdateCommentUseCase"/>
public sealed class UpdateCommentUseCase(
    ICommentRepository comments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IUpdateCommentUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(
        Guid commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await comments.FindForUpdateAsync(commentId, cancellationToken)
                      ?? throw new NotFoundException("Comment", commentId);

        // The "only the author may edit" rule lives on the entity, not here, so it holds no matter
        // which code path reaches the comment.
        comment.Edit(request.Body, currentUser.RequireUserId(), clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
