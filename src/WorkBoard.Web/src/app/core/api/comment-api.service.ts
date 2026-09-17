import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { CommentView, PagedResult } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** Comment threads on tasks. */
@Injectable({ providedIn: 'root' })
export class CommentApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(taskId: string): Promise<PagedResult<CommentView>> {
    return firstValueFrom(
      this.http.get<PagedResult<CommentView>>(this.scope.url(`/tasks/${taskId}/comments`)),
    );
  }

  add(taskId: string, body: string): Promise<CommentView> {
    return firstValueFrom(
      this.http.post<CommentView>(this.scope.url(`/tasks/${taskId}/comments`), { body }),
    );
  }

  delete(commentId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.scope.url(`/comments/${commentId}`)));
  }
}
