import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ProjectApi } from '../../core/api/project-api.service';
import type { ProjectSummary } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { ProjectsComponent } from './projects.component';

/**
 * A stand-in for the projects endpoint.
 *
 * Possible because the component depends on `ProjectApi` and nothing else from the HTTP layer. It
 * used to inject the single `WorkBoardApi`, so faking it meant stubbing forty methods across ten
 * resources — which is why this screen had no test at all before the split.
 */
class FakeProjectApi {
  projects: ProjectSummary[] = [];
  created: { name: string; key: string; description: string | null }[] = [];
  archived: string[] = [];
  failListWith: unknown = null;

  list(): Promise<ProjectSummary[]> {
    return this.failListWith ? Promise.reject(this.failListWith) : Promise.resolve(this.projects);
  }

  create(name: string, key: string, description: string | null): Promise<ProjectSummary> {
    this.created.push({ name, key, description });
    const project = summary({ id: `p-${this.created.length}`, name, key });
    this.projects = [...this.projects, project];
    return Promise.resolve(project);
  }

  archive(projectId: string): Promise<void> {
    this.archived.push(projectId);
    this.projects = this.projects.filter((project) => project.id !== projectId);
    return Promise.resolve();
  }
}

class FakeToastService {
  readonly successes: string[] = [];
  readonly failures: string[] = [];

  success(message: string): void {
    this.successes.push(message);
  }

  fromError(_error: unknown, fallback: string): void {
    this.failures.push(fallback);
  }
}

function summary(overrides: Partial<ProjectSummary> = {}): ProjectSummary {
  return {
    id: 'p-1',
    name: 'WorkBoard',
    key: 'WB',
    description: null,
    isArchived: false,
    taskCount: 0,
    openTaskCount: 0,
    defaultBoardId: 'b-1',
    createdAtUtc: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

interface ProjectsInternals {
  form: {
    setValue(value: { name: string; key: string; description: string }): void;
    invalid: boolean;
  };
  create(): Promise<void>;
  archive(project: ProjectSummary): Promise<void>;
  projects(): ProjectSummary[];
  loading(): boolean;
  showForm(): boolean;
  toggleForm(): void;
}

/**
 * Lets an async `ngOnInit` finish before asserting on the DOM.
 *
 * The application runs zoneless, so `whenStable()` settles only the work Angular knows about — and a
 * bare promise chain inside a lifecycle hook is not that. Yielding to the macrotask queue first lets
 * the fetch resolve; the render that follows is then what the assertions see.
 */
async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  fixture.detectChanges();
  await fixture.whenStable();
}

describe('ProjectsComponent', () => {
  let api: FakeProjectApi;
  let toasts: FakeToastService;

  function render() {
    api = new FakeProjectApi();
    toasts = new FakeToastService();

    TestBed.configureTestingModule({
      imports: [ProjectsComponent],
      providers: [
        // The cards link to their board, so RouterLink needs a router to resolve against. An empty
        // route table is enough: nothing here navigates.
        provideRouter([]),
        { provide: ProjectApi, useValue: api },
        { provide: ToastService, useValue: toasts },
        {
          // Only the three members the template reads. A real store would need HTTP behind it, and
          // what this screen needs from it is a name, an id and whether to show the admin actions.
          provide: WorkspaceStore,
          useValue: { current: () => summary(), requireId: () => 'ws-1', isAdmin: () => true },
        },
      ],
    });

    return TestBed.createComponent(ProjectsComponent);
  }

  it('lists the projects it loaded', async () => {
    const fixture = render();
    api.projects = [
      summary({ id: 'p-1', name: 'WorkBoard' }),
      summary({ id: 'p-2', name: 'Atlas' }),
    ];

    fixture.detectChanges();
    await settle(fixture);

    // The heading renders the key alongside the name, so this asserts on containment rather than
    // on an exact string — the point is that both projects made it onto the page.
    const headings = [...fixture.nativeElement.querySelectorAll('article h2')].map((node) =>
      (node as HTMLElement).textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(headings).toEqual(['WB WorkBoard', 'WB Atlas']);
  });

  /**
   * An empty list rendered as blank space reads as a broken page rather than a new workspace.
   */
  it('shows the empty state when there are no projects', async () => {
    const fixture = render();

    fixture.detectChanges();
    await settle(fixture);

    expect(fixture.nativeElement.querySelector('wb-empty')).not.toBeNull();
  });

  it('shows the loading state until the first response arrives', () => {
    const fixture = render();

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('wb-loading')).not.toBeNull();
  });

  /**
   * The key is part of every task key the project will ever produce, so the server stores it upper
   * case. Uppercasing on the way out means a user typing "wb" and a user typing "WB" cannot create
   * two projects that render identically.
   */
  it('uppercases the project key before sending it', async () => {
    const fixture = render();
    fixture.detectChanges();
    await settle(fixture);

    const component = fixture.componentInstance as unknown as ProjectsInternals;
    component.form.setValue({ name: 'Atlas', key: 'at', description: '' });
    await component.create();

    expect(api.created).toEqual([{ name: 'Atlas', key: 'AT', description: null }]);
    expect(toasts.successes).toContain('Created Atlas.');
  });

  it('does not call the API when the form is invalid', async () => {
    const fixture = render();
    fixture.detectChanges();
    await settle(fixture);

    const component = fixture.componentInstance as unknown as ProjectsInternals;

    // A one-character key fails the minimum length; nothing should reach the server.
    component.form.setValue({ name: 'Atlas', key: 'a', description: '' });
    await component.create();

    expect(api.created).toEqual([]);
  });

  /**
   * A failed load has to say so. Leaving the spinner up forever is the failure mode that gets
   * reported as "the page is slow".
   */
  it('reports a failed load and stops loading', async () => {
    const fixture = render();
    api.failListWith = new Error('offline');

    fixture.detectChanges();
    await settle(fixture);

    expect(toasts.failures).toContain('Could not load projects.');
    expect(fixture.nativeElement.querySelector('wb-loading')).toBeNull();
  });

  it('archives only after the confirmation is accepted', async () => {
    const fixture = render();
    api.projects = [summary({ id: 'p-9', name: 'Legacy' })];
    fixture.detectChanges();
    await settle(fixture);

    const component = fixture.componentInstance as unknown as ProjectsInternals;
    const project = summary({ id: 'p-9', name: 'Legacy' });

    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    await component.archive(project);
    expect(api.archived).toEqual([]);

    confirmSpy.mockReturnValue(true);
    await component.archive(project);
    expect(api.archived).toEqual(['p-9']);

    confirmSpy.mockRestore();
  });
});
