import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

/**
 * The error mapping is the part of this service worth testing: it is what decides whether a user
 * sees the domain's own sentence or a raw HTTP status.
 */
describe('ToastService', () => {
  let toasts: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    toasts = TestBed.inject(ToastService);
  });

  it('surfaces the detail from a problem-details response', () => {
    toasts.fromError(
      new HttpErrorResponse({
        status: 422,
        error: {
          detail: 'A subtask cannot be due after its parent task.',
          correlationId: 'abc123',
        },
      }),
    );

    const [toast] = toasts.toasts();
    expect(toast.kind).toBe('error');
    expect(toast.message).toBe('A subtask cannot be due after its parent task.');
    expect(toast.correlationId).toBe('abc123');
  });

  it('flattens validation errors into one message', () => {
    toasts.fromError(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'One or more validation errors occurred.',
          errors: {
            Password: ['Password must be at least 12 characters.'],
            Email: ["'Email' is not a valid email address."],
          },
        },
      }),
    );

    const [toast] = toasts.toasts();
    expect(toast.message).toContain('at least 12 characters');
    expect(toast.message).toContain('not a valid email address');
  });

  it('explains a 409 in terms of what the user should do', () => {
    toasts.fromError(new HttpErrorResponse({ status: 409, error: null }));

    expect(toasts.toasts()[0].message).toContain('Reload');
  });

  it('distinguishes an unreachable server from a server error', () => {
    // status 0 means the request never arrived. Saying "0" or "server error" would point at the
    // wrong thing entirely.
    toasts.fromError(new HttpErrorResponse({ status: 0 }));

    expect(toasts.toasts()[0].message).toContain('Could not reach the server');
  });

  it('handles a plain Error', () => {
    toasts.fromError(new Error('Something specific broke'));

    expect(toasts.toasts()[0].message).toBe('Something specific broke');
  });

  it('dismisses by id', () => {
    toasts.success('one');
    toasts.success('two');

    const first = toasts.toasts()[0];
    toasts.dismiss(first.id);

    expect(toasts.toasts()).toHaveLength(1);
    expect(toasts.toasts()[0].message).toBe('two');
  });
});
