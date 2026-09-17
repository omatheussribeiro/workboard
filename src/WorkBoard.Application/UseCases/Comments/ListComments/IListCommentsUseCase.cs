using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;

namespace WorkBoard.Application.UseCases.Comments.ListComments;

/// <summary>Reads one task's comment thread.</summary>
public interface IListCommentsUseCase
{
    /// <summary>One page of the thread, oldest first.</summary>
    Task<PagedResult<CommentDto>> ExecuteAsync(Guid taskId, PageRequest page, CancellationToken cancellationToken);
}
