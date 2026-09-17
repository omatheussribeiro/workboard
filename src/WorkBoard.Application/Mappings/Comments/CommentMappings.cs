using System.Linq.Expressions;
using WorkBoard.Application.DTOs.Comments;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Mappings.Comments;

/// <summary>Entity-to-DTO projection for comments.</summary>
public static class CommentMappings
{
    /// <summary>Projects a comment onto its client representation.</summary>
    /// <param name="currentUserId">Decides <c>CanEdit</c>; compared inside SQL, not after loading.</param>
    public static Expression<Func<Comment, CommentDto>> ToDto(Guid currentUserId) =>
        comment => new CommentDto(
            comment.Id,
            comment.TaskId,
            comment.AuthorId,
            comment.Author.DisplayName,
            comment.Body,
            comment.CreatedAtUtc,
            comment.EditedAtUtc,
            comment.AuthorId == currentUserId);
}
