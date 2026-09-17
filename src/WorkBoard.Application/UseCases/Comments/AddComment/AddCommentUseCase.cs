using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.UseCases.Comments.GetComment;

namespace WorkBoard.Application.UseCases.Comments.AddComment;

/// <inheritdoc cref="IAddCommentUseCase"/>
/// <remarks>
/// The comment is created <em>by the task</em> rather than constructed here, so the
/// <c>TaskCommented</c> domain event is raised on the aggregate that owns it. That event is what
/// produces the activity entry and the notifications for the assignees — building a <c>Comment</c>
/// directly would persist the row and silently skip both.
/// </remarks>
public sealed class AddCommentUseCase(
    ITaskRepository tasks,
    IGetCommentUseCase getComment,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IAddCommentUseCase
{
    /// <inheritdoc/>
    public async Task<CommentDto> ExecuteAsync(
        Guid taskId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var authorId = currentUser.RequireUserId();

        var task = await tasks.FindAsync(taskId, cancellationToken)
                   ?? throw new NotFoundException("Task", taskId);

        var comment = task.AddComment(request.Body, authorId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await getComment.ExecuteAsync(comment.Id, cancellationToken);
    }
}
