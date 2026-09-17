import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { ActivityView, PagedResult } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** The change history, workspace-wide or filtered to one project or card. */
@Injectable({ providedIn: 'root' })
export class ActivityApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(
    options: { projectId?: string; taskId?: string; pageSize?: number } = {},
  ): Promise<PagedResult<ActivityView>> {
    return firstValueFrom(
      this.http.get<PagedResult<ActivityView>>(this.scope.url('/activities'), {
        params: WorkspaceScope.params({
          projectId: options.projectId,
          taskId: options.taskId,
          pageSize: options.pageSize ?? 30,
        }),
      }),
    );
  }
}
