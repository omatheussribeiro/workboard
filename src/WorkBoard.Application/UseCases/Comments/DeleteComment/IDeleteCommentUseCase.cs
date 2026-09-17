namespace WorkBoard.Application.UseCases.Comments.DeleteComment;

/// <summary>Soft-deletes a comment. The author or a workspace admin may do so.</summary>
public interface IDeleteCommentUseCase
{
    /// <summary>Marks the comment deleted.</summary>
    Task ExecuteAsync(Guid commentId, CancellationToken cancellationToken);
}
