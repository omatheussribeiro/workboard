import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { SearchResults } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** The command-palette search. Rate-limited server-side, because it is called from a keystroke. */
@Injectable({ providedIn: 'root' })
export class SearchApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  search(term: string): Promise<SearchResults> {
    return firstValueFrom(
      this.http.get<SearchResults>(this.scope.url('/search'), {
        params: WorkspaceScope.params({ q: term }),
      }),
    );
  }
}
