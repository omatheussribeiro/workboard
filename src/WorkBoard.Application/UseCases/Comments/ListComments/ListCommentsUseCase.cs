using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Comments;

namespace WorkBoard.Application.UseCases.Comments.ListComments;

/// <inheritdoc cref="IListCommentsUseCase"/>
public sealed class ListCommentsUseCase(IWorkBoardDbContext db, ICurrentUser currentUser) : IListCommentsUseCase
{
    /// <inheritdoc/>
    public async Task<PagedResult<CommentDto>> ExecuteAsync(
        Guid taskId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .NotDeleted()
            .Where(comment => comment.TaskId == taskId)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ThenBy(comment => comment.Id)
            .Select(CommentMappings.ToDto(currentUser.RequireUserId()))
            .ToPagedResultAsync(page, cancellationToken);
}
