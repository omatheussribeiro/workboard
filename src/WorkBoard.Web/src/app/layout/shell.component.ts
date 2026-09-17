import type { OnInit } from '@angular/core';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NotificationApi } from '../core/api/notification-api.service';
import { AuthService } from '../core/auth/auth.service';
import { ToastService } from '../core/notifications/toast.service';
import { WorkspaceStore } from '../core/workspace/workspace.store';
import { ToastHostComponent } from '../shared/components/toast-host/toast-host.component';
import { LoadingStateComponent } from '../shared/components/loading-state/loading-state.component';

/**
 * The application frame: sidebar, workspace selector, notifications and the routed outlet.
 *
 * Loading the workspace list here rather than in each feature means every screen below can assume a
 * workspace is selected, and the selector is the single place that changes it.
 */
@Component({
  selector: 'wb-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastHostComponent, LoadingStateComponent],
  templateUrl: './shell.component.html',
})
export class ShellComponent implements OnInit {
  private readonly notificationApi = inject(NotificationApi);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  protected readonly workspaces = inject(WorkspaceStore);
  protected readonly user = this.auth.user;
  protected readonly unreadCount = signal(0);
  protected readonly sidebarOpen = signal(false);
  protected readonly ready = signal(false);
  protected readonly signingOut = signal(false);

  protected readonly initials = computed(() => initialsOf(this.user()?.displayName ?? '?'));

  async ngOnInit(): Promise<void> {
    try {
      await this.workspaces.load();

      if (this.workspaces.currentId()) {
        await this.refreshUnreadCount();
      }
    } catch (error) {
      this.toasts.fromError(error, 'Could not load your workspaces.');
    } finally {
      // Set even on failure: leaving a permanent spinner hides the error the user needs to see.
      this.ready.set(true);
    }
  }

  protected async onWorkspaceChange(event: Event): Promise<void> {
    const id = (event.target as HTMLSelectElement).value;
    this.workspaces.select(id);

    // Back to the dashboard: the current URL may hold ids belonging to the previous workspace, which
    // would now resolve to 404s.
    await this.router.navigate(['/dashboard']);
    await this.refreshUnreadCount();
  }

  protected toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  protected closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  protected async signOut(): Promise<void> {
    if (this.signingOut()) {
      return;
    }

    this.signingOut.set(true);

    try {
      await this.auth.logout();
    } catch (error) {
      this.signingOut.set(false);
      this.toasts.fromError(error, 'Could not finish signing out.');
    }
  }

  private async refreshUnreadCount(): Promise<void> {
    try {
      const { count } = await this.notificationApi.unreadCount();
      this.unreadCount.set(count);
    } catch {
      // A badge is not worth a toast. Failing quietly leaves the rest of the shell usable.
      this.unreadCount.set(0);
    }
  }
}

function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
}
