namespace WorkBoard.Application.DTOs.Common;

/// <summary>A page of results plus the total, so a client can render a pager.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Total number of pages at the current page size.</summary>
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>Whether another page follows this one.</summary>
    public bool HasNextPage => Page < TotalPages;
}
