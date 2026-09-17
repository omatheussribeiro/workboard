import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardApi } from '../../core/api/dashboard-api.service';
import type { DashboardView } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { MetricCardComponent } from '../../shared/components/metric-card/metric-card.component';

@Component({
  selector: 'wb-dashboard',
  imports: [
    DatePipe,
    RouterLink,
    PriorityBadgeComponent,
    EmptyStateComponent,
    PageHeaderComponent,
    LoadingStateComponent,
    MetricCardComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly dashboardApi = inject(DashboardApi);
  private readonly toasts = inject(ToastService);
  protected readonly workspaces = inject(WorkspaceStore);

  protected readonly data = signal<DashboardView | null>(null);
  protected readonly loading = signal(true);

  /** The tallest bar in the throughput window, so the rest can be drawn relative to it. */
  protected readonly peakThroughput = computed(() =>
    Math.max(1, ...(this.data()?.throughput ?? []).map((point) => point.completed)),
  );

  async ngOnInit(): Promise<void> {
    // The dashboard doubles as first-run onboarding. Avoid building a workspace-scoped API URL
    // before the user has created their first workspace.
    if (!this.workspaces.currentId()) {
      this.loading.set(false);
      return;
    }

    try {
      this.data.set(await this.dashboardApi.get());
    } catch (error) {
      this.toasts.fromError(error, 'Could not load the dashboard.');
    } finally {
      this.loading.set(false);
    }
  }

  protected barHeight(completed: number): string {
    // A day with no completions still gets a visible sliver, so the axis reads as a timeline rather
    // than as gaps where nothing rendered.
    const ratio = completed / this.peakThroughput();
    return `${Math.max(4, Math.round(ratio * 100))}%`;
  }

  protected percentDone(total: number, done: number): number {
    return total === 0 ? 0 : Math.round((done / total) * 100);
  }

  protected statusCount(category: string): number {
    return this.data()?.byStatus.find((slice) => slice.category === category)?.count ?? 0;
  }
}
