import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/notifications/toast.service';

/** Renders the toast queue. Mounted once, by the shell. */
@Component({
  selector: 'wb-toast-host',
  // aria-live="polite" so a screen reader announces a toast without interrupting what the user is
  // doing. "assertive" would cut across their current context for what is usually a confirmation.
  templateUrl: './toast-host.component.html',
  styleUrl: './toast-host.component.scss',
})
export class ToastHostComponent {
  protected readonly toasts = inject(ToastService);
}
