import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type {
  CreateTaskBody,
  MoveTaskBody,
  PagedResult,
  TaskDetail,
  TaskListItem,
  TaskQuery,
  UpdateTaskBody,
} from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** Tasks: the list, the detail, and everything that changes one. */
@Injectable({ providedIn: 'root' })
export class TaskApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(query: TaskQuery): Promise<PagedResult<TaskListItem>> {
    return firstValueFrom(
      this.http.get<PagedResult<TaskListItem>>(this.scope.url('/tasks'), {
        params: WorkspaceScope.params({ ...query }),
      }),
    );
  }

  get(taskId: string): Promise<TaskDetail> {
    return firstValueFrom(this.http.get<TaskDetail>(this.scope.url(`/tasks/${taskId}`)));
  }

  create(body: CreateTaskBody): Promise<TaskDetail> {
    return firstValueFrom(this.http.post<TaskDetail>(this.scope.url('/tasks'), body));
  }

  /** Requires the concurrency token from the last read; a stale one comes back as 409. */
  update(taskId: string, body: UpdateTaskBody): Promise<TaskDetail> {
    return firstValueFrom(this.http.put<TaskDetail>(this.scope.url(`/tasks/${taskId}`), body));
  }

  /** Applies a drop. See {@link MoveTaskBody} for why it sends neighbours rather than an index. */
  move(taskId: string, body: MoveTaskBody): Promise<TaskDetail> {
    return firstValueFrom(
      this.http.put<TaskDetail>(this.scope.url(`/tasks/${taskId}/position`), body),
    );
  }

  assign(taskId: string, userId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(this.scope.url(`/tasks/${taskId}/assignees`), { userId }),
    );
  }

  unassign(taskId: string, userId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(this.scope.url(`/tasks/${taskId}/assignees/${userId}`)),
    );
  }

  applyLabel(taskId: string, labelId: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(this.scope.url(`/tasks/${taskId}/labels/${labelId}`), {}),
    );
  }

  removeLabel(taskId: string, labelId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(this.scope.url(`/tasks/${taskId}/labels/${labelId}`)),
    );
  }

  delete(taskId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.scope.url(`/tasks/${taskId}`)));
  }
}
