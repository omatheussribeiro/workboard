using WorkBoard.Application.DTOs.Comments;

namespace WorkBoard.Application.UseCases.Comments.GetComment;

/// <summary>Reads one comment.</summary>
public interface IGetCommentUseCase
{
    /// <summary>Returns the comment.</summary>
    /// <exception cref="WorkBoard.Application.Exceptions.NotFoundException">Unknown or deleted comment.</exception>
    Task<CommentDto> ExecuteAsync(Guid commentId, CancellationToken cancellationToken);
}
