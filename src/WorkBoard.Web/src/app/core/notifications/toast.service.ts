import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import type { ProblemDetails } from '../models';

export interface Toast {
  id: number;
  kind: 'success' | 'error' | 'info';
  message: string;
  /** Shown in small print so a user can quote it in a bug report. */
  correlationId?: string;
}

const DISMISS_AFTER_MS = 6000;

/**
 * The one place a failure becomes something a user reads.
 *
 * It understands the API's problem-details shape, so a 422 arrives as the domain's own sentence
 * ("A subtask cannot be due after its parent") rather than as "Http failure response ... 422". The
 * correlation id is carried through, which is what turns a screenshot into something traceable in
 * the logs.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly items = signal<Toast[]>([]);

  readonly toasts = this.items.asReadonly();

  success(message: string): void {
    this.push({ kind: 'success', message });
  }

  info(message: string): void {
    this.push({ kind: 'info', message });
  }

  error(message: string, correlationId?: string): void {
    this.push({ kind: 'error', message, correlationId });
  }

  /** Turns any thrown value into a message worth showing. */
  fromError(error: unknown, fallback = 'Something went wrong.'): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.error(error instanceof Error ? error.message : fallback);
      return;
    }

    // status 0 means the request never reached the server: offline, DNS, CORS. Saying "0" helps
    // nobody, and the fix is different from any server-side failure.
    if (error.status === 0) {
      this.error('Could not reach the server. Check your connection and try again.');
      return;
    }

    const problem = error.error as ProblemDetails | null;

    if (problem?.errors) {
      // Validation: show the field messages themselves, which are written for a user.
      const messages = Object.values(problem.errors).flat();
      this.error(messages.join(' '), problem.correlationId);
      return;
    }

    const message = problem?.detail ?? problem?.title ?? describeStatus(error.status, fallback);
    this.error(message, problem?.correlationId);
  }

  dismiss(id: number): void {
    this.items.update((current) => current.filter((toast) => toast.id !== id));
  }

  private push(toast: Omit<Toast, 'id'>): void {
    const id = this.nextId++;
    this.items.update((current) => [...current, { ...toast, id }]);

    setTimeout(() => this.dismiss(id), DISMISS_AFTER_MS);
  }
}

function describeStatus(status: number, fallback: string): string {
  switch (status) {
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have access to that.';
    case 404:
      return 'That no longer exists.';
    case 409:
      return 'Someone else changed this first. Reload and try again.';
    case 429:
      return 'Too many requests. Wait a moment and try again.';
    default:
      return fallback;
  }
}
