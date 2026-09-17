namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>
/// Orderings the task list offers.
/// </summary>
/// <remarks>
/// A closed enum rather than a free-text <c>sort</c> parameter: an arbitrary column name from the
/// query string either has to be validated against an allow-list anyway, or it is an injection point
/// the moment anyone builds SQL from it. Each member also maps to an index that exists.
/// </remarks>
public enum TaskSort
{
    /// <summary>Most recently created first. The default.</summary>
    Newest = 0,

    /// <summary>Oldest created first.</summary>
    Oldest = 1,

    /// <summary>Soonest deadline first, cards without one last.</summary>
    DueDate = 2,

    /// <summary>Highest priority first, then by deadline.</summary>
    Priority = 3,
}
