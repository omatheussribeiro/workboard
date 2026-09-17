using WorkBoard.Application.DTOs.Comments;

namespace WorkBoard.Application.UseCases.Comments.UpdateComment;

/// <summary>Edits a comment. Only its author may do so.</summary>
public interface IUpdateCommentUseCase
{
    /// <summary>Applies the edit.</summary>
    Task ExecuteAsync(Guid commentId, UpdateCommentRequest request, CancellationToken cancellationToken);
}
