namespace WorkBoard.Application.DTOs.Tasks;

/// <summary>
/// A drag-and-drop drop event.
/// </summary>
/// <remarks>
/// The client sends the ids of the cards the task landed between, not an index. An index would be
/// computed against the board as the dragging client last saw it, and would silently land in the
/// wrong slot if anyone else reordered the column meanwhile. Neighbour ids let the server derive the
/// position from current state, and <see cref="RowVersion"/> catches the case where the dragged card
/// itself changed underneath.
/// </remarks>
/// <param name="TargetColumnId">Column the card was dropped into; may be the one it came from.</param>
/// <param name="PreviousTaskId">Card immediately above the drop point, if any.</param>
/// <param name="NextTaskId">Card immediately below the drop point, if any.</param>
/// <param name="RowVersion">The <c>rowversion</c> of the dragged card as the client last read it.</param>
public sealed record MoveTaskRequest(
    Guid TargetColumnId,
    Guid? PreviousTaskId,
    Guid? NextTaskId,
    byte[] RowVersion);
