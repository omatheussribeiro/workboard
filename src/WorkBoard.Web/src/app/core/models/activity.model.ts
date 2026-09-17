/** One entry of the change history. */
export interface ActivityView {
  id: string;
  type: string;
  summary: string;
  actorId: string;
  actorName: string;
  projectId: string | null;
  taskId: string | null;
  payloadJson: string | null;
  occurredAtUtc: string;
}
