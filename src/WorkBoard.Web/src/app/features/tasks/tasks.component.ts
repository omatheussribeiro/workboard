import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BoardApi } from '../../core/api/board-api.service';
import { LabelApi } from '../../core/api/label-api.service';
import { ProjectApi } from '../../core/api/project-api.service';
import { TaskApi } from '../../core/api/task-api.service';
import type {
  LabelView,
  BoardColumn,
  TaskQuery,
  PagedResult,
  ProjectSummary,
  TaskListItem,
  TaskPriority,
  TaskSort,
  TaskStatusCategory,
} from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { AssigneeListComponent } from '../../shared/components/assignee-list/assignee-list.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

/**
 * The filterable, paged task list.
 *
 * Filtering happens on the server, not in the browser. Fetching every task and filtering client-side
 * is simpler and stops working the moment a workspace has more tasks than fit in one response — and
 * it throws away every index the database has.
 */
@Component({
  selector: 'wb-tasks',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    PriorityBadgeComponent,
    AssigneeListComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    LoadingStateComponent,
  ],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.scss',
})
export class TasksComponent implements OnInit {
  private readonly boardApi = inject(BoardApi);
  private readonly labelApi = inject(LabelApi);
  private readonly projectApi = inject(ProjectApi);
  private readonly taskApi = inject(TaskApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  private searchDebounce?: ReturnType<typeof setTimeout>;

  protected readonly result = signal<PagedResult<TaskListItem> | null>(null);
  protected readonly projects = signal<ProjectSummary[]>([]);
  protected readonly labels = signal<LabelView[]>([]);
  protected readonly loading = signal(true);
  protected readonly showCreateForm = signal(false);
  protected readonly loadingColumns = signal(false);
  protected readonly creating = signal(false);
  protected readonly createColumns = signal<BoardColumn[]>([]);

  protected readonly newTaskProjectId = signal('');
  protected readonly newTaskColumnId = signal('');
  protected readonly newTaskTitle = signal('');
  protected readonly newTaskDescription = signal('');
  protected readonly newTaskPriority = signal<TaskPriority>('Medium');
  protected readonly newTaskDueAt = signal('');

  protected readonly filterProjectId = signal('');
  protected readonly filterCategory = signal<TaskStatusCategory | ''>('');
  protected readonly filterPriority = signal<TaskPriority | ''>('');
  protected readonly filterLabelId = signal('');
  protected readonly filterOverdue = signal(false);
  protected readonly search = signal('');
  protected readonly sort = signal<TaskSort>('Newest');
  protected readonly page = signal(1);

  async ngOnInit(): Promise<void> {
    // Arriving from a project card pre-selects that project, so the link means what it looked like.
    const projectId = this.route.snapshot.queryParamMap.get('projectId');
    if (projectId) {
      this.filterProjectId.set(projectId);
    }

    await Promise.all([this.loadFilters(), this.load()]);
  }

  protected async applyFilters(): Promise<void> {
    this.page.set(1);
    await this.load();
  }

  protected async toggleCreateForm(): Promise<void> {
    if (this.showCreateForm()) {
      this.resetCreateForm();
      return;
    }

    this.showCreateForm.set(true);

    const preferredProject =
      this.projects().find((project) => project.id === this.filterProjectId()) ??
      this.projects().find((project) => project.defaultBoardId !== null);

    if (preferredProject) {
      await this.selectCreateProject(preferredProject.id);
    }
  }

  protected async selectCreateProject(projectId: string): Promise<void> {
    this.newTaskProjectId.set(projectId);
    this.newTaskColumnId.set('');
    this.createColumns.set([]);

    const project = this.projects().find((item) => item.id === projectId);
    if (!project?.defaultBoardId) {
      return;
    }

    this.loadingColumns.set(true);

    try {
      const board = await this.boardApi.get(project.defaultBoardId);

      // The user may change projects before the first board request finishes. Do not let that older
      // response replace the columns belonging to the currently selected project.
      if (this.newTaskProjectId() !== projectId) {
        return;
      }

      this.createColumns.set(board.columns);
      const firstColumn =
        board.columns.find((column) => column.category === 'Todo') ?? board.columns[0];
      this.newTaskColumnId.set(firstColumn?.id ?? '');
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the project board.');
    } finally {
      if (this.newTaskProjectId() === projectId) {
        this.loadingColumns.set(false);
      }
    }
  }

  protected async createTask(): Promise<void> {
    const title = this.newTaskTitle().trim();
    const projectId = this.newTaskProjectId();
    const boardColumnId = this.newTaskColumnId();

    if (!title || !projectId || !boardColumnId || this.creating()) {
      return;
    }

    const dueAt = this.newTaskDueAt();
    this.creating.set(true);

    try {
      const created = await this.taskApi.create({
        projectId,
        boardColumnId,
        title,
        description: this.newTaskDescription().trim() || null,
        priority: this.newTaskPriority(),
        dueAtUtc: dueAt ? new Date(dueAt).toISOString() : null,
        parentTaskId: null,
        assigneeIds: null,
        labelIds: null,
      });

      this.toasts.success(`Created ${created.key}.`);
      this.resetCreateForm();
      await this.router.navigate(['/tasks', created.id]);
    } catch (error) {
      this.toasts.fromError(error, 'Could not create the task.');
    } finally {
      this.creating.set(false);
    }
  }

  /**
   * Debounces the search box.
   *
   * Every keystroke would otherwise be a request against a `LIKE '%term%'` scan, and the responses
   * can arrive out of order — so the list would flicker between results for different prefixes.
   */
  protected onSearchInput(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => void this.applyFilters(), 300);
  }

  protected async goToPage(page: number): Promise<void> {
    this.page.set(page);
    await this.load();
  }

  protected async clearFilters(): Promise<void> {
    this.filterProjectId.set('');
    this.filterCategory.set('');
    this.filterPriority.set('');
    this.filterLabelId.set('');
    this.filterOverdue.set(false);
    this.search.set('');
    this.sort.set('Newest');

    await this.applyFilters();
  }

  protected hasFilters(): boolean {
    return (
      this.filterProjectId() !== '' ||
      this.filterCategory() !== '' ||
      this.filterPriority() !== '' ||
      this.filterLabelId() !== '' ||
      this.filterOverdue() ||
      this.search().trim() !== ''
    );
  }

  private async load(): Promise<void> {
    this.loading.set(true);

    const query: TaskQuery = {
      projectId: this.filterProjectId() || undefined,
      category: this.filterCategory() || undefined,
      priority: this.filterPriority() || undefined,
      labelId: this.filterLabelId() || undefined,
      overdue: this.filterOverdue() || undefined,
      search: this.search().trim() || undefined,
      sort: this.sort(),
      page: this.page(),
      pageSize: 25,
    };

    try {
      this.result.set(await this.taskApi.list(query));
    } catch (error) {
      this.toasts.fromError(error, 'Could not load tasks.');
    } finally {
      this.loading.set(false);
    }
  }

  private async loadFilters(): Promise<void> {
    try {
      const [projects, labels] = await Promise.all([this.projectApi.list(), this.labelApi.list()]);
      this.projects.set(projects);
      this.labels.set(labels);
    } catch {
      // The filter dropdowns are a convenience. Losing them should not stop the list rendering.
    }
  }

  private resetCreateForm(): void {
    this.showCreateForm.set(false);
    this.createColumns.set([]);
    this.newTaskProjectId.set('');
    this.newTaskColumnId.set('');
    this.newTaskTitle.set('');
    this.newTaskDescription.set('');
    this.newTaskPriority.set('Medium');
    this.newTaskDueAt.set('');
  }
}
