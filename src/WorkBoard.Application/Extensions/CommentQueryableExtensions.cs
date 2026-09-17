using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Extensions;

/// <summary>Query helpers for comments.</summary>
public static class CommentQueryableExtensions
{
    /// <summary>
    /// Comments that have not been soft-deleted.
    /// </summary>
    /// <remarks>
    /// Deletion is a flag rather than a row removal, so a thread keeps its shape and an audit trail
    /// survives. Every read has to exclude them, which is exactly the kind of clause that gets
    /// forgotten once — so it is written here and called, rather than retyped in each use case.
    /// </remarks>
    public static IQueryable<Comment> NotDeleted(this IQueryable<Comment> comments) =>
        comments.Where(comment => !comment.IsDeleted);
}
