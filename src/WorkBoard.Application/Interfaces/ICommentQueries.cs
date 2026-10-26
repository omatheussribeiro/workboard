using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;

namespace WorkBoard.Application.Interfaces;

public interface ICommentQueries
{
    Task<CommentDto?> GetAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken);

    Task<PagedResult<CommentDto>> ListAsync(
        Guid taskId,
        Guid currentUserId,
        PageRequest page,
        CancellationToken cancellationToken);
}
