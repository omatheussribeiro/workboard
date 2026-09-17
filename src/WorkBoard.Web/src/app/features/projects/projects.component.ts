import type { OnInit } from '@angular/core';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProjectApi } from '../../core/api/project-api.service';
import type { ProjectSummary } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';

@Component({
  selector: 'wb-projects',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    EmptyStateComponent,
    PageHeaderComponent,
    LoadingStateComponent,
  ],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.scss',
})
export class ProjectsComponent implements OnInit {
  private readonly projectApi = inject(ProjectApi);
  private readonly toasts = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  protected readonly workspaces = inject(WorkspaceStore);
  protected readonly projects = signal<ProjectSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly showForm = signal(false);
  protected readonly busy = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    key: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(10),
        Validators.pattern(/^[A-Za-z]+$/),
      ],
    ],
    description: [''],
  });

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  protected toggleForm(): void {
    this.showForm.update((open) => !open);
  }

  protected async create(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);

    try {
      const { name, key, description } = this.form.getRawValue();
      await this.projectApi.create(name, key.toUpperCase(), description || null);

      this.form.reset();
      this.showForm.set(false);
      await this.load();
      this.toasts.success(`Created ${name}.`);
    } catch (error) {
      this.toasts.fromError(error, 'Could not create the project.');
    } finally {
      this.busy.set(false);
    }
  }

  protected async archive(project: ProjectSummary): Promise<void> {
    // Archiving hides a project for everyone in the workspace, so it is confirmed rather than
    // applied on a single click. It is reversible, which is why a confirm is enough.
    if (!confirm(`Archive ${project.name}? It can be restored later.`)) {
      return;
    }

    try {
      await this.projectApi.archive(project.id);
      await this.load();
      this.toasts.success(`Archived ${project.name}.`);
    } catch (error) {
      this.toasts.fromError(error, 'Could not archive the project.');
    }
  }

  private async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.projects.set(await this.projectApi.list());
    } catch (error) {
      this.toasts.fromError(error, 'Could not load projects.');
    } finally {
      this.loading.set(false);
    }
  }
}
