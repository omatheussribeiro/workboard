/**
 * The API contracts, mirrored in TypeScript.
 *
 * Hand-written rather than generated from the OpenAPI document. For a surface this size a generator
 * adds a build step, a tool version to keep in step, and thousands of lines of generated code to
 * review; hand-written types stay readable and let the client name things the way the UI thinks
 * about them. The trade-off is real and worth naming: nothing checks these against the server, so a
 * contract change is caught by the integration tests and by a failing screen rather than by `tsc`.
 * At a larger surface that trade flips, and the OpenAPI document the API already publishes is what
 * the generator would consume.
 *
 * One file per resource, mirroring the server's feature folders, with the string unions in
 * `core/enums`. This barrel is the import surface, so a consumer writes
 * `import type { BoardView } from '../../core/models'` and does not have to know which file it is
 * in — while the files themselves stay small enough to read in full.
 */
export type { AuthenticatedUser, AuthResponse } from './auth.model';
export type { CardAssignee, CardLabel } from './card.model';
export type { BoardCard, BoardColumn, BoardView } from './board.model';
export type { CommentView } from './comment.model';
export type { LabelView } from './label.model';
export type { ProjectSummary } from './project.model';
export type { PagedResult } from './paging.model';
export type { ProblemDetails } from './problem-details.model';
export type {
  CreateTaskBody,
  MoveTaskBody,
  SubtaskView,
  TaskDetail,
  TaskListItem,
  TaskQuery,
  UpdateTaskBody,
} from './task.model';
export type { WorkspaceMember, WorkspaceSummary } from './workspace.model';
export type {
  DashboardView,
  MyTask,
  PrioritySlice,
  ProjectProgress,
  StatusSlice,
  ThroughputPoint,
  WorkspaceTotals,
} from './dashboard.model';
export type { NotificationView } from './notification.model';
export type { ActivityView } from './activity.model';
export type { SearchHit, SearchResults } from './search.model';
export type { WorkspaceRole } from '../enums/workspace-role';
export type { TaskPriority } from '../enums/task-priority';
export type { TaskSort } from '../enums/task-sort';
export type { TaskStatusCategory } from '../enums/task-status-category';
