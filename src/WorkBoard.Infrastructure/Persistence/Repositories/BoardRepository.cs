using Microsoft.EntityFrameworkCore;
using WorkBoard.Application.Interfaces;
using WorkBoard.Domain.Entities;
using WorkBoard.Infrastructure.Persistence.Context;

namespace WorkBoard.Infrastructure.Persistence.Repositories;

/// <inheritdoc cref="IBoardRepository"/>
public sealed class BoardRepository(WorkBoardDbContext context) : IBoardRepository
{
    /// <inheritdoc/>
    public Task<Board?> FindWithColumnsAsync(Guid boardId, CancellationToken cancellationToken) =>
        context.Boards
            .Include(board => board.Columns)
            .FirstOrDefaultAsync(board => board.Id == boardId, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// One query. The column is projected as an entity, so EF still tracks it — a projection only
    /// stops tracking when it produces something that is not an entity, which is why the column can
    /// be mutated afterwards while the project id costs nothing but a join.
    /// </remarks>
    public Task<ColumnPlacement?> FindColumnPlacementAsync(Guid columnId, CancellationToken cancellationToken) =>
        context.BoardColumns
            .Where(column => column.Id == columnId)
            .Select(column => new ColumnPlacement(column, column.Board.ProjectId))
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<BoardColumn?> FindColumnAsync(Guid columnId, CancellationToken cancellationToken) =>
        context.BoardColumns.FirstOrDefaultAsync(column => column.Id == columnId, cancellationToken);

    /// <inheritdoc/>
    public void Add(Board board) => context.Boards.Add(board);
}
