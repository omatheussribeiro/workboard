import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SearchApi } from '../../core/api/search-api.service';
import type { SearchResults } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'wb-search',
  imports: [FormsModule, RouterLink, EmptyStateComponent, PageHeaderComponent],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss',
})
export class SearchComponent {
  private readonly searchApi = inject(SearchApi);
  private readonly toasts = inject(ToastService);

  private debounce?: ReturnType<typeof setTimeout>;

  /** Guards against an earlier, slower response overwriting a later one. */
  private requestId = 0;

  protected readonly term = signal('');
  protected readonly results = signal<SearchResults | null>(null);
  protected readonly searching = signal(false);

  protected onInput(value: string): void {
    this.term.set(value);
    clearTimeout(this.debounce);

    if (value.trim().length < 2) {
      // The server refuses a one-character term anyway; not sending it saves the round trip.
      this.results.set(null);
      return;
    }

    this.debounce = setTimeout(() => void this.run(), 250);
  }

  private async run(): Promise<void> {
    const id = ++this.requestId;
    this.searching.set(true);

    try {
      const results = await this.searchApi.search(this.term().trim());

      // Responses can arrive out of order. Ignoring anything but the newest keeps the list from
      // flickering back to the results for a shorter prefix.
      if (id === this.requestId) {
        this.results.set(results);
      }
    } catch (error) {
      this.toasts.fromError(error, 'Search failed.');
    } finally {
      if (id === this.requestId) {
        this.searching.set(false);
      }
    }
  }
}
