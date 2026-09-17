using WorkBoard.Domain.Entities;

namespace WorkBoard.Application.Interfaces;

/// <summary>
/// A tracked board column together with the project its board belongs to.
/// </summary>
/// <remarks>
/// The pair exists because creating a task needs both, and reading them separately is either two
/// round trips or an <c>Include</c> of the board purely to reach one <see cref="Guid"/>. Returning
/// the id alongside the entity also keeps the service off <c>column.Board.ProjectId</c> — a
/// navigation that is populated by EF and null on any column the domain built itself, which made the
/// rule impossible to unit-test without a database.
/// </remarks>
/// <param name="Column">The column, tracked, ready to be mutated.</param>
/// <param name="ProjectId">The project owning the board this column belongs to.</param>
public sealed record ColumnPlacement(BoardColumn Column, Guid ProjectId);
