import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { WorkspaceApi } from '../../core/api/workspace-api.service';
import type { WorkspaceSummary } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { CreateWorkspaceComponent } from './create-workspace.component';

const createdWorkspace: WorkspaceSummary = {
  id: 'ws-1',
  name: 'Operações',
  slug: 'operacoes',
  description: null,
  myRole: 'Owner',
  memberCount: 1,
  projectCount: 0,
  createdAtUtc: '2026-01-01T00:00:00Z',
};

class FakeWorkspaceApi {
  readonly created: { name: string; slug: string; description: string | null }[] = [];

  create(name: string, slug: string, description: string | null): Promise<WorkspaceSummary> {
    this.created.push({ name, slug, description });
    return Promise.resolve({ ...createdWorkspace, name, slug, description });
  }
}

interface CreateWorkspaceInternals {
  form: {
    setValue(value: { name: string; slug: string; description: string }): void;
    controls: { name: { setValue(value: string): void }; slug: { value: string } };
  };
  suggestSlug(): void;
  create(): Promise<void>;
}

describe('CreateWorkspaceComponent', () => {
  it('posts the form, selects the workspace and opens its dashboard', async () => {
    const api = new FakeWorkspaceApi();
    const store = { add: vi.fn() };
    const router = { navigate: vi.fn().mockResolvedValue(true) };
    const toasts = { success: vi.fn(), fromError: vi.fn() };

    TestBed.configureTestingModule({
      imports: [CreateWorkspaceComponent],
      providers: [
        { provide: WorkspaceApi, useValue: api },
        { provide: WorkspaceStore, useValue: store },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toasts },
      ],
    });

    const fixture = TestBed.createComponent(CreateWorkspaceComponent);
    const component = fixture.componentInstance as unknown as CreateWorkspaceInternals;
    component.form.setValue({ name: 'Operações', slug: 'operacoes', description: '' });

    await component.create();

    expect(api.created).toEqual([{ name: 'Operações', slug: 'operacoes', description: null }]);
    expect(store.add).toHaveBeenCalledWith(expect.objectContaining({ id: 'ws-1' }));
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    expect(toasts.success).toHaveBeenCalledWith('Created Operações.');
  });

  it('generates an API-safe slug from an accented name', () => {
    TestBed.configureTestingModule({
      imports: [CreateWorkspaceComponent],
      providers: [
        { provide: WorkspaceApi, useValue: new FakeWorkspaceApi() },
        { provide: WorkspaceStore, useValue: { add: vi.fn() } },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ToastService, useValue: { success: vi.fn(), fromError: vi.fn() } },
      ],
    });

    const component = TestBed.createComponent(CreateWorkspaceComponent)
      .componentInstance as unknown as CreateWorkspaceInternals;
    component.form.controls.name.setValue('Operações & Produto');
    component.suggestSlug();

    expect(component.form.controls.slug.value).toBe('operacoes-produto');
  });
});
