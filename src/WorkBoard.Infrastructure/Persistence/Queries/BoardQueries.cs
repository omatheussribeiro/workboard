using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Boards;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Queries;

public sealed class BoardQueries(WorkBoardDbContext context) : IBoardQueries
{
    public Task<BoardDto?> GetAsync(Guid boardId, DateTimeOffset nowUtc, CancellationToken cancellationToken) =>
        context.Boards
            .AsNoTracking()
            .Where(board => board.Id == boardId)
            .Select(BoardMappings.ToBoard(nowUtc))
            .FirstOrDefaultAsync(cancellationToken);
}
