import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { WorkspaceSummary } from '../models';

const SELECTED_KEY = 'workboard.workspaceId';

/**
 * Which workspace the user is currently looking at.
 *
 * Every workspace-scoped URL in the API carries the workspace id in its path, which is what makes
 * the server bind the tenant. Rather than have each feature read the id from the route, it is held
 * here once and every API service reads it — so a component cannot accidentally call one workspace's
 * endpoint while showing another's data.
 *
 * The selection is remembered per browser so a reload lands where the user left off. It is only ever
 * a hint: the server re-checks membership on every request, so a stale or forged id in storage buys
 * nothing.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceStore {
  private readonly http = inject(HttpClient);

  private readonly all = signal<WorkspaceSummary[]>([]);
  private readonly selectedId = signal<string | null>(readSelected());
  private readonly loading = signal(false);

  readonly workspaces = this.all.asReadonly();
  readonly isLoading = this.loading.asReadonly();

  readonly current = computed(() => {
    const id = this.selectedId();
    const list = this.all();

    return list.find((workspace) => workspace.id === id) ?? list[0] ?? null;
  });

  readonly currentId = computed(() => this.current()?.id ?? null);

  readonly isAdmin = computed(() => {
    const role = this.current()?.myRole;
    return role === 'Admin' || role === 'Owner';
  });

  /** Throws rather than returning null: callers reach this only from inside a workspace route. */
  requireId(): string {
    const id = this.currentId();
    if (!id) {
      throw new Error('No workspace is selected.');
    }

    return id;
  }

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      const workspaces = await firstValueFrom(this.http.get<WorkspaceSummary[]>('/api/workspaces'));
      this.all.set(workspaces);

      // The remembered id may name a workspace the user has since been removed from. Falling back to
      // the first one they can see beats rendering an empty screen with no explanation.
      if (!workspaces.some((workspace) => workspace.id === this.selectedId())) {
        this.select(workspaces[0]?.id ?? null);
      }
    } finally {
      this.loading.set(false);
    }
  }

  select(id: string | null): void {
    this.selectedId.set(id);

    try {
      if (id) {
        localStorage.setItem(SELECTED_KEY, id);
      } else {
        localStorage.removeItem(SELECTED_KEY);
      }
    } catch {
      // Storage unavailable; the selection simply does not survive a reload.
    }
  }

  add(workspace: WorkspaceSummary): void {
    this.all.update((current) =>
      [...current, workspace].sort((a, b) => a.name.localeCompare(b.name)),
    );
    this.select(workspace.id);
  }
}

function readSelected(): string | null {
  try {
    return localStorage.getItem(SELECTED_KEY);
  } catch {
    return null;
  }
}
