using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.UseCases.Boards.AddColumn;

/// <summary>Appends a column to a board.</summary>
public interface IAddColumnUseCase
{
    /// <summary>Adds the column and returns its id.</summary>
    Task<Guid> ExecuteAsync(Guid boardId, CreateColumnRequest request, CancellationToken cancellationToken);
}
