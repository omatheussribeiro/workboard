import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { LabelView } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** The workspace label taxonomy. Creating and deleting require the admin role server-side. */
@Injectable({ providedIn: 'root' })
export class LabelApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(): Promise<LabelView[]> {
    return firstValueFrom(this.http.get<LabelView[]>(this.scope.url('/labels')));
  }

  create(name: string, colorHex: string): Promise<LabelView> {
    return firstValueFrom(this.http.post<LabelView>(this.scope.url('/labels'), { name, colorHex }));
  }

  delete(labelId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(this.scope.url(`/labels/${labelId}`)));
  }
}
