
namespace WorkBoard.Domain.Enums;

/// <summary>
/// The semantic bucket a column belongs to.
/// </summary>
/// <remarks>
/// Teams rename and add columns freely ("Code Review", "QA", "Blocked"), but reporting needs a
/// stable notion of not-started / in-flight / finished. Rather than inferring that from column
/// names, each column declares its category once; the dashboard aggregates on the category, so
/// renaming a column never breaks a metric.
/// </remarks>
public enum TaskStatusCategory
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
}
