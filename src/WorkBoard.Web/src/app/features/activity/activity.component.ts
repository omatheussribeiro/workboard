import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ActivityApi } from '../../core/api/activity-api.service';
import type { ActivityView } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'wb-activity',
  imports: [DatePipe, RouterLink, EmptyStateComponent, PageHeaderComponent, LoadingStateComponent],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.scss',
})
export class ActivityComponent implements OnInit {
  private readonly activityApi = inject(ActivityApi);
  private readonly toasts = inject(ToastService);

  protected readonly entries = signal<ActivityView[]>([]);
  protected readonly loading = signal(true);

  async ngOnInit(): Promise<void> {
    try {
      const feed = await this.activityApi.list({ pageSize: 50 });
      this.entries.set(feed.items);
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the activity feed.');
    } finally {
      this.loading.set(false);
    }
  }

  protected initials(name: string): string {
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  }
}
