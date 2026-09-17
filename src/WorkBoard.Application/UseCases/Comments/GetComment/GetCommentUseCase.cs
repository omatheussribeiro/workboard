using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Extensions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Comments;

namespace WorkBoard.Application.UseCases.Comments.GetComment;

/// <inheritdoc cref="IGetCommentUseCase"/>
public sealed class GetCommentUseCase(IWorkBoardDbContext db, ICurrentUser currentUser) : IGetCommentUseCase
{
    /// <inheritdoc/>
    public async Task<CommentDto> ExecuteAsync(Guid commentId, CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .NotDeleted()
            .Where(comment => comment.Id == commentId)
            .Select(CommentMappings.ToDto(currentUser.RequireUserId()))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Comment", commentId);
}
