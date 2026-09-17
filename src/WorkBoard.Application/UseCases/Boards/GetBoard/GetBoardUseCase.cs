using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;
using WorkBoard.Application.Mappings.Boards;

namespace WorkBoard.Application.UseCases.Boards.GetBoard;

/// <inheritdoc cref="IGetBoardUseCase"/>
/// <remarks>
/// <c>AsNoTracking</c> is strictly redundant on a query that projects to a DTO — EF tracks nothing it
/// cannot identify as an entity — but it is written anyway, so that adding an entity to the
/// projection later does not silently start tracking a few hundred cards per request.
/// </remarks>
public sealed class GetBoardUseCase(IWorkBoardDbContext db, IClock clock) : IGetBoardUseCase
{
    /// <inheritdoc/>
    public async Task<BoardDto> ExecuteAsync(Guid boardId, CancellationToken cancellationToken) =>
        await db.Boards
            .AsNoTracking()
            .Where(board => board.Id == boardId)
            .Select(BoardMappings.ToBoard(clock.UtcNow))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Board", boardId);
}
