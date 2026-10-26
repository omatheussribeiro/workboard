using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Comments;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class CommentQueries(WorkBoardDbContext context) : ICommentQueries
{
    public async Task<CommentDto?> GetAsync(
        Guid commentId,
        Guid currentUserId,
        CancellationToken cancellationToken) =>
        await context.Comments
            .AsNoTracking()
            .NotDeleted()
            .Where(comment => comment.Id == commentId)
            .Select(CommentMappings.ToDto(currentUserId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<CommentDto>> ListAsync(
        Guid taskId,
        Guid currentUserId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await context.Comments
            .AsNoTracking()
            .NotDeleted()
            .Where(comment => comment.TaskId == taskId)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .Select(CommentMappings.ToDto(currentUserId))
            .ToPagedResultAsync(page, cancellationToken);
}
