using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.UseCases.Boards.MoveColumn;

/// <summary>Reorders a column.</summary>
public interface IMoveColumnUseCase
{
    /// <summary>Moves the column and renumbers the rest.</summary>
    Task ExecuteAsync(Guid boardId, Guid columnId, MoveColumnRequest request, CancellationToken cancellationToken);
}
