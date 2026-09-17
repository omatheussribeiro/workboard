import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { DashboardView } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** The workspace dashboard, which the API assembles in a single response. */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  get(): Promise<DashboardView> {
    return firstValueFrom(this.http.get<DashboardView>(this.scope.url('/dashboard')));
  }
}
