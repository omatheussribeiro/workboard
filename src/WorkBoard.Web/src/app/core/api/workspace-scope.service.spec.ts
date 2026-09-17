import { TestBed } from '@angular/core/testing';
import { WorkspaceScope } from './workspace-scope.service';
import { WorkspaceStore } from '../workspace/workspace.store';

class FakeWorkspaceStore {
  id: string | null = 'ws-1';

  requireId(): string {
    if (!this.id) {
      throw new Error('No workspace selected.');
    }
    return this.id;
  }
}

describe('WorkspaceScope', () => {
  function create(): { scope: WorkspaceScope; store: FakeWorkspaceStore } {
    const store = new FakeWorkspaceStore();
    TestBed.configureTestingModule({
      providers: [WorkspaceScope, { provide: WorkspaceStore, useValue: store }],
    });

    return { scope: TestBed.inject(WorkspaceScope), store };
  }

  it('prefixes a path with the selected workspace', () => {
    const { scope } = create();
    expect(scope.url('/projects')).toBe('/api/workspaces/ws-1/projects');
  });

  /**
   * No caller passes a workspace id, so there is nowhere for the wrong one to come from — but there
   * is somewhere for *none* to come from, and a URL with `undefined` in it would 404 in a way that
   * looks like a routing bug rather than a missing selection.
   */
  it('refuses to build a URL when no workspace is selected', () => {
    const { scope, store } = create();
    store.id = null;

    expect(() => scope.url('/projects')).toThrow();
  });

  it('omits empty and undefined filters instead of sending them blank', () => {
    const params = WorkspaceScope.params({
      projectId: 'p-1',
      priority: undefined,
      search: '',
      overdue: false,
      page: 0,
    });

    expect(params.get('projectId')).toBe('p-1');
    expect(params.has('priority')).toBe(false);
    expect(params.has('search')).toBe(false);
  });

  /**
   * `false` and `0` are values, not absences. Filtering on truthiness instead of on
   * null/undefined/'' would silently drop `overdue=false` and `page=0`.
   */
  it('keeps falsy values that are still values', () => {
    const params = WorkspaceScope.params({ overdue: false, page: 0 });

    expect(params.get('overdue')).toBe('false');
    expect(params.get('page')).toBe('0');
  });
});
