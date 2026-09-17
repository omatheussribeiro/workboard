import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { NotificationView, PagedResult } from '../models';
import { WorkspaceScope } from './workspace-scope.service';

/** In-app notifications for the signed-in user. */
@Injectable({ providedIn: 'root' })
export class NotificationApi {
  private readonly http = inject(HttpClient);
  private readonly scope = inject(WorkspaceScope);

  list(unreadOnly = false): Promise<PagedResult<NotificationView>> {
    return firstValueFrom(
      this.http.get<PagedResult<NotificationView>>(this.scope.url('/notifications'), {
        params: WorkspaceScope.params({ unreadOnly }),
      }),
    );
  }

  unreadCount(): Promise<{ count: number }> {
    return firstValueFrom(
      this.http.get<{ count: number }>(this.scope.url('/notifications/unread-count')),
    );
  }

  markRead(notificationId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(this.scope.url(`/notifications/${notificationId}/read`), {}),
    );
  }

  markAllRead(): Promise<{ updated: number }> {
    return firstValueFrom(
      this.http.post<{ updated: number }>(this.scope.url('/notifications/read-all'), {}),
    );
  }
}
