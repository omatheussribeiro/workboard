import type { TaskPriority } from '../enums/task-priority';
import type { TaskStatusCategory } from '../enums/task-status-category';
import type { CardAssignee, CardLabel } from './card.model';

/** A card as rendered on the Kanban board — deliberately narrower than the task detail. */
export interface BoardCard {
  id: string;
  key: string;
  title: string;
  priority: TaskPriority;
  position: number;
  dueAtUtc: string | null;
  isOverdue: boolean;
  commentCount: number;
  subtaskCount: number;
  completedSubtaskCount: number;
  assignees: CardAssignee[];
  labels: CardLabel[];

  /** Base64 concurrency token; sent back on a drop so a stale drag is rejected with 409. */
  rowVersion: string;
}

/** One column of a board. */
export interface BoardColumn {
  id: string;
  name: string;
  position: number;
  category: TaskStatusCategory;
  wipLimit: number | null;
  taskCount: number;
  cards: BoardCard[];
}

/** A whole board, as one request returns it. */
export interface BoardView {
  id: string;
  name: string;
  projectId: string;
  projectKey: string;
  columns: BoardColumn[];
}
