/** A project in the sidebar and the project list. */
export interface ProjectSummary {
  id: string;
  name: string;
  key: string;
  description: string | null;
  isArchived: boolean;
  taskCount: number;
  openTaskCount: number;
  defaultBoardId: string | null;
  createdAtUtc: string;
}
