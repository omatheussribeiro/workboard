using WorkBoard.Application.DTOs.Boards;
using WorkBoard.Application.Exceptions;
using WorkBoard.Application.Interfaces;

namespace WorkBoard.Application.UseCases.Boards.GetBoard;

/// <inheritdoc cref="IGetBoardUseCase"/>
/// <remarks>
/// <c>AsNoTracking</c> is strictly redundant on a query that projects to a DTO — EF tracks nothing it
/// cannot identify as an entity — but it is written anyway, so that adding an entity to the
/// projection later does not silently start tracking a few hundred cards per request.
/// </remarks>
public sealed class GetBoardUseCase(IBoardQueries queries, IClock clock) : IGetBoardUseCase
{
    /// <inheritdoc/>
    public async Task<BoardDto> ExecuteAsync(Guid boardId, CancellationToken cancellationToken) =>
        await queries.GetAsync(boardId, clock.UtcNow, cancellationToken)
        ?? throw new NotFoundException("Board", boardId);
}
