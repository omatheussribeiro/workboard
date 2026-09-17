import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { WorkspaceMember, WorkspaceRole, WorkspaceSummary } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/**
 * Workspaces and their membership.
 *
 * One service per resource rather than the single `WorkBoardApi` this used to be. That one file held
 * forty methods across ten resources, so every feature imported the whole API to use three calls of
 * it — and any change to a task method invalidated the mental model of everyone reading the label
 * methods. Split by resource, a feature depends on exactly what it uses, and each file is short
 * enough to read top to bottom.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  /** The workspaces the caller belongs to. Not scoped: there is no workspace selected yet. */
  list(): Promise<WorkspaceSummary[]> {
    return firstValueFrom(this.http.get<WorkspaceSummary[]>('/api/workspaces'));
  }

  create(name: string, slug: string, description: string | null): Promise<WorkspaceSummary> {
    return firstValueFrom(
      this.http.post<WorkspaceSummary>('/api/workspaces', { name, slug, description }),
    );
  }

  listMembers(): Promise<WorkspaceMember[]> {
    return firstValueFrom(this.http.get<WorkspaceMember[]>(this.scope.url('/members')));
  }

  addMember(email: string, role: WorkspaceRole): Promise<WorkspaceMember> {
    return firstValueFrom(
      this.http.post<WorkspaceMember>(this.scope.url('/members'), { email, role }),
    );
  }

  changeMemberRole(userId: string, role: WorkspaceRole): Promise<void> {
    return firstValueFrom(this.http.put<void>(this.scope.url(`/members/${userId}`), { role }));
  }

  removeMember(userId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.scope.url(`/members/${userId}`)));
  }
}
