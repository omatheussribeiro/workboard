import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { ProjectSummary } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** Projects inside the selected workspace. */
@Injectable({ providedIn: 'root' })
export class ProjectApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(includeArchived = false): Promise<ProjectSummary[]> {
    return firstValueFrom(
      this.http.get<ProjectSummary[]>(this.scope.url('/projects'), {
        params: WorkspaceScope.params({ includeArchived }),
      }),
    );
  }

  create(name: string, key: string, description: string | null): Promise<ProjectSummary> {
    return firstValueFrom(
      this.http.post<ProjectSummary>(this.scope.url('/projects'), { name, key, description }),
    );
  }

  update(projectId: string, name: string, description: string | null): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(this.scope.url(`/projects/${projectId}`), { name, description }),
    );
  }

  /** Archives rather than deletes — the API has no destructive project route. */
  archive(projectId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.scope.url(`/projects/${projectId}`)));
  }
}
