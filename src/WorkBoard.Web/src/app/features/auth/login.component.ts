import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/notifications/toast.service';

@Component({
  selector: 'wb-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toasts = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected invalid(control: 'email' | 'password'): boolean {
    const field = this.form.controls[control];
    return field.invalid && (field.dirty || field.touched);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      // Marking the whole form touched is what makes the messages appear: until a control is touched
      // an invalid field shows nothing, which on submit looks like a button that does nothing.
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);

    try {
      const { email, password } = this.form.getRawValue();
      await this.auth.login(email, password);

      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      await this.router.navigateByUrl(returnUrl);
    } catch (error) {
      this.toasts.fromError(error, 'Could not sign in.');
    } finally {
      this.busy.set(false);
    }
  }
}
