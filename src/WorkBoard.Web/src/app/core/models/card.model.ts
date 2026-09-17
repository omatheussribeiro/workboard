/**
 * The two shapes shared by every view that renders a card.
 *
 * Kept together and apart from the views that use them, because the board, the task list and the
 * task detail all render the same avatar and the same chip — and previously each declared its own
 * copy of these two interfaces.
 */

/** Someone assigned to a card. `email` is present on the task views and omitted on the board. */
export interface CardAssignee {
  userId: string;
  displayName: string;
  email?: string;
}

/** A label as a coloured chip. */
export interface CardLabel {
  id: string;
  name: string;
  colorHex: string;
}
