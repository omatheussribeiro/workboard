import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { WorkspaceApi } from '../../core/api/workspace-api.service';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'wb-create-workspace',
  imports: [ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './create-workspace.component.html',
  styleUrl: './create-workspace.component.scss',
})
export class CreateWorkspaceComponent {
  private readonly workspaceApi = inject(WorkspaceApi);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);
  private readonly workspaces = inject(WorkspaceStore);
  private readonly fb = inject(FormBuilder);

  protected readonly busy = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    slug: [
      '',
      [
        Validators.required,
        Validators.pattern(/^[a-z0-9]+(-[a-z0-9]+)*$/),
        Validators.maxLength(60),
      ],
    ],
    description: [''],
  });

  protected slugInvalid(): boolean {
    const slug = this.form.controls.slug;
    return slug.invalid && (slug.dirty || slug.touched);
  }

  /**
   * Fills the slug from the name until the user edits it themselves.
   *
   * The `pristine` check is what makes this bearable: once someone has typed a slug, the name no
   * longer overwrites it. Without that, editing the name silently discards their choice.
   */
  protected suggestSlug(): void {
    if (!this.form.controls.slug.pristine) {
      return;
    }

    const slug = this.form.controls.name.value
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 60);

    this.form.controls.slug.setValue(slug, { emitEvent: false });
  }

  protected async create(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);

    try {
      const { name, slug, description } = this.form.getRawValue();
      const workspace = await this.workspaceApi.create(name, slug, description || null);

      // Added to the store and selected, so the shell switches to it without another round trip.
      this.workspaces.add(workspace);
      this.toasts.success(`Created ${workspace.name}.`);
      await this.router.navigate(['/dashboard']);
    } catch (error) {
      this.toasts.fromError(error, 'Could not create the workspace.');
    } finally {
      this.busy.set(false);
    }
  }
}
