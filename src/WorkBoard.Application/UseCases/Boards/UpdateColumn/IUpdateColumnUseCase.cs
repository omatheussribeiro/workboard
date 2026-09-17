using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.UseCases.Boards.UpdateColumn;

/// <summary>Renames a column and updates its category and limit.</summary>
public interface IUpdateColumnUseCase
{
    /// <summary>Applies the change.</summary>
    Task ExecuteAsync(
        Guid boardId,
        Guid columnId,
        UpdateColumnRequest request,
        CancellationToken cancellationToken);
}
