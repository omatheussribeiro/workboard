import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ActivityApi } from '../../core/api/activity-api.service';
import { CommentApi } from '../../core/api/comment-api.service';
import { LabelApi } from '../../core/api/label-api.service';
import { TaskApi } from '../../core/api/task-api.service';
import { WorkspaceApi } from '../../core/api/workspace-api.service';
import type {
  ActivityView,
  CommentView,
  LabelView,
  TaskDetail,
  TaskPriority,
  WorkspaceMember,
} from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { AssigneeListComponent } from '../../shared/components/assignee-list/assignee-list.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LabelChipComponent } from '../../shared/components/label-chip/label-chip.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'wb-task-detail',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    PriorityBadgeComponent,
    LabelChipComponent,
    AssigneeListComponent,
    EmptyStateComponent,
    LoadingStateComponent,
  ],
  templateUrl: './task-detail.component.html',
  styleUrl: './task-detail.component.scss',
})
export class TaskDetailComponent implements OnInit {
  private readonly activityApi = inject(ActivityApi);
  private readonly commentApi = inject(CommentApi);
  private readonly labelApi = inject(LabelApi);
  private readonly taskApi = inject(TaskApi);
  private readonly workspaceApi = inject(WorkspaceApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  protected readonly task = signal<TaskDetail | null>(null);
  protected readonly comments = signal<CommentView[]>([]);
  protected readonly history = signal<ActivityView[]>([]);
  protected readonly members = signal<WorkspaceMember[]>([]);
  protected readonly labels = signal<LabelView[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  protected readonly editing = signal(false);
  protected readonly draftTitle = signal('');
  protected readonly draftDescription = signal('');
  protected readonly draftPriority = signal<TaskPriority>('Medium');
  protected readonly draftDue = signal('');
  protected readonly newComment = signal('');

  async ngOnInit(): Promise<void> {
    const taskId = this.route.snapshot.paramMap.get('taskId');
    if (!taskId) {
      return;
    }

    await this.load(taskId);
  }

  protected startEditing(): void {
    const task = this.task();
    if (!task) {
      return;
    }

    this.draftTitle.set(task.title);
    this.draftDescription.set(task.description ?? '');
    this.draftPriority.set(task.priority);
    // <input type="date"> speaks yyyy-MM-dd and nothing else.
    this.draftDue.set(task.dueAtUtc ? task.dueAtUtc.slice(0, 10) : '');
    this.editing.set(true);
  }

  protected cancelEditing(): void {
    this.editing.set(false);
  }

  /**
   * Saves the edit, carrying the concurrency token the task was loaded with.
   *
   * A 409 here means someone else saved first. The toast says so, and the task is reloaded so the
   * user sees their colleague's version rather than continuing to edit a copy that can no longer be
   * written.
   */
  protected async save(): Promise<void> {
    const task = this.task();
    if (!task) {
      return;
    }

    this.saving.set(true);

    try {
      const updated = await this.taskApi.update(task.id, {
        title: this.draftTitle().trim(),
        description: this.draftDescription().trim() || null,
        priority: this.draftPriority(),
        // The date input gives a local calendar day; the API wants an instant. End of day keeps
        // "due on the 5th" from meaning "due at midnight, so overdue all of the 5th".
        dueAtUtc: this.draftDue() ? new Date(`${this.draftDue()}T23:59:59Z`).toISOString() : null,
        rowVersion: task.rowVersion,
      });

      this.task.set(updated);
      this.editing.set(false);
      this.toasts.success('Saved.');
      await this.loadHistory(updated.id);
    } catch (error) {
      this.toasts.fromError(error, 'Could not save the task.');
      await this.load(task.id);
    } finally {
      this.saving.set(false);
    }
  }

  protected async addComment(): Promise<void> {
    const task = this.task();
    const body = this.newComment().trim();

    if (!task || !body) {
      return;
    }

    try {
      const comment = await this.commentApi.add(task.id, body);
      this.comments.update((current) => [...current, comment]);
      this.newComment.set('');
      await this.loadHistory(task.id);
    } catch (error) {
      this.toasts.fromError(error, 'Could not post the comment.');
    }
  }

  protected async deleteComment(comment: CommentView): Promise<void> {
    try {
      await this.commentApi.delete(comment.id);
      this.comments.update((current) => current.filter((c) => c.id !== comment.id));
    } catch (error) {
      this.toasts.fromError(error, 'Could not delete the comment.');
    }
  }

  protected async assign(event: Event): Promise<void> {
    const select = event.target as HTMLSelectElement;
    const userId = select.value;
    const task = this.task();

    if (!userId || !task) {
      return;
    }

    // Reset immediately: the dropdown is an action, not a bound value, and leaving it on the chosen
    // name suggests a state that does not exist.
    select.value = '';

    try {
      await this.taskApi.assign(task.id, userId);
      this.task.set(await this.taskApi.get(task.id));
    } catch (error) {
      this.toasts.fromError(error, 'Could not assign the task.');
    }
  }

  protected async unassign(userId: string): Promise<void> {
    const task = this.task();
    if (!task) {
      return;
    }

    try {
      await this.taskApi.unassign(task.id, userId);
      this.task.set(await this.taskApi.get(task.id));
    } catch (error) {
      this.toasts.fromError(error, 'Could not unassign.');
    }
  }

  protected async toggleLabel(label: LabelView): Promise<void> {
    const task = this.task();
    if (!task) {
      return;
    }

    const applied = task.labels.some((l) => l.id === label.id);

    try {
      if (applied) {
        await this.taskApi.removeLabel(task.id, label.id);
      } else {
        await this.taskApi.applyLabel(task.id, label.id);
      }

      this.task.set(await this.taskApi.get(task.id));
    } catch (error) {
      this.toasts.fromError(error, 'Could not change the labels.');
    }
  }

  protected hasLabel(labelId: string): boolean {
    return this.task()?.labels.some((label) => label.id === labelId) ?? false;
  }

  protected unassignedMembers(): WorkspaceMember[] {
    const assigned = new Set(this.task()?.assignees.map((a) => a.userId) ?? []);
    return this.members().filter((member) => !assigned.has(member.userId));
  }

  protected async deleteTask(): Promise<void> {
    const task = this.task();
    if (!task || !confirm(`Delete ${task.key}? This cannot be undone.`)) {
      return;
    }

    try {
      await this.taskApi.delete(task.id);
      this.toasts.success(`Deleted ${task.key}.`);
      await this.router.navigate(['/tasks']);
    } catch (error) {
      this.toasts.fromError(error, 'Could not delete the task.');
    }
  }

  private async load(taskId: string): Promise<void> {
    this.loading.set(true);

    try {
      // Fired together: they are independent reads, and doing them in sequence would make the page
      // wait for the sum of four round trips instead of the slowest one.
      const [task, comments, members, labels] = await Promise.all([
        this.taskApi.get(taskId),
        this.commentApi.list(taskId),
        this.workspaceApi.listMembers(),
        this.labelApi.list(),
      ]);

      this.task.set(task);
      this.comments.set(comments.items);
      this.members.set(members);
      this.labels.set(labels);

      await this.loadHistory(taskId);
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the task.');
      this.task.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadHistory(taskId: string): Promise<void> {
    try {
      const history = await this.activityApi.list({ taskId, pageSize: 20 });
      this.history.set(history.items);
    } catch {
      // History is supporting detail; losing it should not blank the page.
    }
  }
}
