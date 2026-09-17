using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Enums;

namespace WorkBoard.Application.UseCases.Comments.DeleteComment;

/// <inheritdoc cref="IDeleteCommentUseCase"/>
public sealed class DeleteCommentUseCase(
    ICommentRepository comments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IDeleteCommentUseCase
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(Guid commentId, CancellationToken cancellationToken)
    {
        var comment = await comments.FindForUpdateAsync(commentId, cancellationToken)
                      ?? throw new NotFoundException("Comment", commentId);

        comment.Delete(currentUser.RequireUserId(), currentUser.HasAtLeast(WorkspaceRole.Admin));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
