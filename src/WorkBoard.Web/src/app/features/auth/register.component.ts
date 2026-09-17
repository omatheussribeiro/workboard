import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/notifications/toast.service';

@Component({
  selector: 'wb-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(12),
        Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/),
      ],
    ],
  });

  protected invalid(control: 'displayName' | 'email' | 'password'): boolean {
    const field = this.form.controls[control];
    return field.invalid && (field.dirty || field.touched);
  }

  protected passwordError(): string {
    const errors = this.form.controls.password.errors ?? {};

    if (errors['required']) {
      return 'Choose a password.';
    }
    if (errors['minlength']) {
      return 'At least 12 characters.';
    }

    return 'Include at least one letter and one digit.';
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);

    try {
      const { email, displayName, password } = this.form.getRawValue();
      await this.auth.register(email, displayName, password);

      // Registration signs the user in, so they land in the app rather than on the login page.
      await this.router.navigate(['/dashboard']);
    } catch (error) {
      this.toasts.fromError(error, 'Could not create your account.');
    } finally {
      this.busy.set(false);
    }
  }
}
