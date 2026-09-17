import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { BoardView, TaskStatusCategory } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** The Kanban board and its columns. */
@Injectable({ providedIn: 'root' })
export class BoardApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  /** The whole board — columns, cards, assignees and labels — in one request. */
  get(boardId: string): Promise<BoardView> {
    return firstValueFrom(this.http.get<BoardView>(this.scope.url(`/boards/${boardId}`)));
  }

  addColumn(
    boardId: string,
    name: string,
    category: TaskStatusCategory,
    wipLimit: number | null,
  ): Promise<{ columnId: string }> {
    return firstValueFrom(
      this.http.post<{ columnId: string }>(this.scope.url(`/boards/${boardId}/columns`), {
        name,
        category,
        wipLimit,
      }),
    );
  }

  removeColumn(boardId: string, columnId: string): Promise<void> {
    return firstValueFrom(
      this.http.delete<void>(this.scope.url(`/boards/${boardId}/columns/${columnId}`)),
    );
  }
}
