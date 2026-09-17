using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Common;

namespace WorkBoard.Application.Extensions;

/// <summary>Query helpers shared by the read-side handlers.</summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Runs the count and the page as two queries and packages the result.
    /// </summary>
    /// <remarks>
    /// The count runs against the already-filtered, unordered, unprojected query so SQL Server can
    /// satisfy it from an index rather than materialising rows. Ordering must be applied by the
    /// caller before this point: without a deterministic ORDER BY, OFFSET/FETCH is free to return
    /// the same row on two different pages.
    /// </remarks>
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

    /// <summary>Applies a predicate only when <paramref name="condition"/> holds, keeping filter chains flat.</summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        System.Linq.Expressions.Expression<Func<T, bool>> predicate) =>
        condition ? query.Where(predicate) : query;
}
