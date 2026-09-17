import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { WorkspaceStore } from '../workspace/workspace.store';

/**
 * Builds workspace-scoped URLs, and nothing else.
 *
 * Every tenant-owned route starts `/api/workspaces/{id}/…`. Having each API service interpolate that
 * itself would mean eleven copies of the same template string and eleven chances to pass a workspace
 * id from somewhere other than the store — which is the one place that knows which workspace the user
 * actually selected. Injecting this instead means no caller ever handles a workspace id.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceScope {
  private readonly workspaces = inject(WorkspaceStore);

  /** Prefixes a path with the current workspace. Throws when no workspace is selected. */
  url(path: string): string {
    return `/api/workspaces/${this.workspaces.requireId()}${path}`;
  }

  /**
   * Turns an object into query parameters, dropping anything empty.
   *
   * Undefined and empty values are omitted rather than sent blank: the API applies a filter only
   * when the parameter is present, so `priority=` would filter on the empty string and return
   * nothing, which looks exactly like a bug in the list.
   */
  static params(values: Record<string, unknown>): HttpParams {
    let params = new HttpParams();

    for (const [key, value] of Object.entries(values)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return params;
  }
}
