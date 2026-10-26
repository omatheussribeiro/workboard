using WorkBoard.Application.DTOs.Boards;

namespace WorkBoard.Application.Interfaces;

public interface IBoardQueries
{
    Task<BoardDto?> GetAsync(Guid boardId, DateTimeOffset nowUtc, CancellationToken cancellationToken);
}
