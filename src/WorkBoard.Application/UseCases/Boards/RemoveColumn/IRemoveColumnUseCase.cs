namespace WorkBoard.Application.UseCases.Boards.RemoveColumn;

/// <summary>Removes an empty column.</summary>
public interface IRemoveColumnUseCase
{
    /// <summary>Removes it. The aggregate refuses while the column still holds cards.</summary>
    Task ExecuteAsync(Guid boardId, Guid columnId, CancellationToken cancellationToken);
}
