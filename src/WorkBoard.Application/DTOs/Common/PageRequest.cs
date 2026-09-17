namespace WorkBoard.Application.DTOs.Common;

/// <summary>
/// Paging parameters, clamped on construction.
/// </summary>
/// <remarks>
/// <c>PageSize</c> is capped at <see cref="MaxPageSize"/> rather than trusted. An uncapped page size
/// is the easiest way to turn a harmless list endpoint into a way to pull an entire table in one
/// request, and it defeats every index the query would otherwise use.
/// </remarks>
public sealed record PageRequest
{
    /// <summary>The largest page any caller can ask for.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Creates a page request, clamping both values into their valid ranges.</summary>
    public PageRequest(int page = 1, int pageSize = 25)
    {
        Page = page < 1 ? 1 : page;
        PageSize = Math.Clamp(pageSize, 1, MaxPageSize);
    }

    /// <summary>One-based page number.</summary>
    public int Page { get; }

    /// <summary>Rows per page, never above <see cref="MaxPageSize"/>.</summary>
    public int PageSize { get; }

    /// <summary>Rows to skip to reach this page.</summary>
    public int Skip => (Page - 1) * PageSize;
}
