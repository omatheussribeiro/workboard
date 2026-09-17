import { DatePipe } from '@angular/common';
import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationApi } from '../../core/api/notification-api.service';
import type { NotificationView } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'wb-notifications',
  imports: [DatePipe, RouterLink, EmptyStateComponent, PageHeaderComponent, LoadingStateComponent],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss',
})
export class NotificationsComponent implements OnInit {
  private readonly notificationApi = inject(NotificationApi);
  private readonly toasts = inject(ToastService);

  protected readonly items = signal<NotificationView[]>([]);
  protected readonly loading = signal(true);

  protected unreadCount(): number {
    return this.items().filter((item) => !item.isRead).length;
  }

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected async markRead(item: NotificationView): Promise<void> {
    try {
      await this.notificationApi.markRead(item.id);
      this.items.update((current) =>
        current.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)),
      );
    } catch (error) {
      this.toasts.fromError(error, 'Could not mark it read.');
    }
  }

  protected async markAllRead(): Promise<void> {
    try {
      await this.notificationApi.markAllRead();
      this.items.update((current) => current.map((n) => ({ ...n, isRead: true })));
    } catch (error) {
      this.toasts.fromError(error, 'Could not mark them read.');
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);

    try {
      const page = await this.notificationApi.list();
      this.items.set(page.items);
    } catch (error) {
      this.toasts.fromError(error, 'Could not load notifications.');
    } finally {
      this.loading.set(false);
    }
  }
}
