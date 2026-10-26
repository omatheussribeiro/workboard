using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;
using WorkBoard.Domain.Entities;

namespace WorkBoard.Infrastructure.Persistence.Queries;

internal static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }

    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;
}

internal static class CommentQueryableExtensions
{
    public static IQueryable<Comment> NotDeleted(this IQueryable<Comment> comments) =>
        comments.Where(comment => !comment.IsDeleted);
}
