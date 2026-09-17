using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.UseCases.Boards.GetBoard;

/// <summary>Reads a whole Kanban board.</summary>
public interface IGetBoardUseCase
{
    /// <summary>Columns and cards, in one round trip.</summary>
    /// <exception cref="WorkBoard.Application.Exceptions.NotFoundException">Unknown board, or it belongs to another workspace.</exception>
    Task<BoardDto> ExecuteAsync(Guid boardId, CancellationToken cancellationToken);
}
