/** One comment in a task thread. */
export interface CommentView {
  id: string;
  taskId: string;
  authorId: string;
  authorName: string;
  body: string;
  createdAtUtc: string;
  editedAtUtc: string | null;

  /** Decided by the server, so the client does not re-derive the rule. */
  canEdit: boolean;
}
