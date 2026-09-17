
namespace WorkBoard.Domain.Enums;

/// <summary>Ordered so that "at least High" is a comparison. Persisted as int.</summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
}
