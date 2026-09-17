using WorkBoard.Application.DTOs.Comments;

namespace WorkBoard.Application.UseCases.Comments.AddComment;

/// <summary>Posts a comment on a task.</summary>
public interface IAddCommentUseCase
{
    /// <summary>Posts it and returns it as the thread will render it.</summary>
    Task<CommentDto> ExecuteAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken);
}
