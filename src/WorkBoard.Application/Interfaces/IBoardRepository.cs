using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>Write-side access to the board aggregate.</summary>
public interface IBoardRepository
{
    /// <summary>
    /// Loads a board tracked with its columns.
    /// </summary>
    /// <remarks>
    /// Always with the columns: every mutating method on <see cref="Board"/> reorders or validates
    /// against the full set, so loading it without them would let the aggregate compute positions
    /// from an empty collection and silently corrupt the order.
    /// </remarks>
    Task<Board?> FindWithColumnsAsync(Guid boardId, CancellationToken cancellationToken);

    /// <summary>Loads a column tracked, together with the id of the project its board belongs to.</summary>
    Task<ColumnPlacement?> FindColumnPlacementAsync(Guid columnId, CancellationToken cancellationToken);

    /// <summary>Loads a column tracked, without its board.</summary>
    Task<BoardColumn?> FindColumnAsync(Guid columnId, CancellationToken cancellationToken);

    /// <summary>Stages a new board.</summary>
    void Add(Board board);
}
