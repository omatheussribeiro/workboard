import type { TaskPriority } from '../enums/task-priority';
import type { TaskStatusCategory } from '../enums/task-status-category';

/** Headline counters across the workspace. */
export interface WorkspaceTotals {
  projects: number;
  tasks: number;
  openTasks: number;
  overdueTasks: number;
  members: number;
}

/** One slice of the status chart. */
export interface StatusSlice {
  category: TaskStatusCategory;
  count: number;
}

/** One slice of the priority chart. */
export interface PrioritySlice {
  priority: TaskPriority;
  count: number;
}

/** One project's progress bar. */
export interface ProjectProgress {
  projectId: string;
  name: string;
  key: string;
  total: number;
  done: number;
}

/** One day of the throughput chart. Days with no completions are present with a zero. */
export interface ThroughputPoint {
  date: string;
  completed: number;
}

/** One of the caller's open cards. */
export interface MyTask {
  id: string;
  key: string;
  title: string;
  priority: TaskPriority;
  dueAtUtc: string | null;
  isOverdue: boolean;
  columnName: string;
}

/** Everything the dashboard renders, in one response. */
export interface DashboardView {
  totals: WorkspaceTotals;
  byStatus: StatusSlice[];
  byPriority: PrioritySlice[];
  projects: ProjectProgress[];
  throughput: ThroughputPoint[];
  myOpenTasks: MyTask[];
}
