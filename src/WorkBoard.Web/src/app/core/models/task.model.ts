import type { TaskPriority } from '../enums/task-priority';
import type { TaskSort } from '../enums/task-sort';
import type { TaskStatusCategory } from '../enums/task-status-category';
import type { CardAssignee, CardLabel } from './card.model';

/** A child card, as listed inside its parent. */
export interface SubtaskView {
  id: string;
  key: string;
  title: string;
  isCompleted: boolean;
  priority: TaskPriority;
}

/** The full card, as the detail panel renders it. */
export interface TaskDetail {
  id: string;
  key: string;
  projectId: string;
  projectName: string;
  boardColumnId: string;
  columnName: string;
  category: TaskStatusCategory;
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueAtUtc: string | null;
  isOverdue: boolean;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  parentTaskId: string | null;
  parentTaskTitle: string | null;
  createdById: string;
  createdByName: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  assignees: CardAssignee[];
  labels: CardLabel[];
  subtasks: SubtaskView[];
  rowVersion: string;
}

/** A card in a list or a search result. */
export interface TaskListItem {
  id: string;
  key: string;
  title: string;
  projectId: string;
  projectKey: string;
  columnName: string;
  category: TaskStatusCategory;
  priority: TaskPriority;
  dueAtUtc: string | null;
  isOverdue: boolean;
  assignees: CardAssignee[];
}

/**
 * The query string behind the task list.
 *
 * Every field optional, and omitted rather than sent empty: the API applies a filter only when it is
 * present, so sending `priority=` would be a filter on the empty string rather than no filter.
 */
export interface TaskQuery {
  projectId?: string;
  columnId?: string;
  category?: TaskStatusCategory;
  priority?: TaskPriority;
  assigneeId?: string;
  labelId?: string;
  overdue?: boolean;
  search?: string;
  sort?: TaskSort;
  page?: number;
  pageSize?: number;
}

/** Body of a create-task request. */
export interface CreateTaskBody {
  projectId: string;
  boardColumnId: string;
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueAtUtc: string | null;
  parentTaskId?: string | null;
  assigneeIds?: string[] | null;
  labelIds?: string[] | null;
}

/** Body of an update-task request. Carries the concurrency token from the last read. */
export interface UpdateTaskBody {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueAtUtc: string | null;
  rowVersion: string;
}

/**
 * Body of a drag-and-drop move.
 *
 * Neighbour ids rather than an index, because an index is computed against the board as this client
 * last saw it. `rowVersion` is the card's concurrency token: if anyone else has touched it since,
 * the server answers 409 instead of overwriting their change.
 */
export interface MoveTaskBody {
  targetColumnId: string;
  previousTaskId: string | null;
  nextTaskId: string | null;
  rowVersion: string;
}
