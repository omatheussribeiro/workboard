import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { WorkspaceSummary } from '../models';
import { WorkspaceStore } from './workspace.store';

function workspace(id: string, name: string): WorkspaceSummary {
  return {
    id,
    name,
    slug: name.toLowerCase(),
    description: null,
    myRole: 'Member',
    memberCount: 1,
    projectCount: 0,
    createdAtUtc: '2026-01-01T00:00:00Z',
  };
}

describe('WorkspaceStore', () => {
  let store: WorkspaceStore;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    store = TestBed.inject(WorkspaceStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('selects the first workspace when nothing is remembered', async () => {
    const loading = store.load();
    http.expectOne('/api/workspaces').flush([workspace('a', 'Acme'), workspace('b', 'Beta')]);
    await loading;

    expect(store.currentId()).toBe('a');
  });

  it('remembers the selection across a reload', async () => {
    const loading = store.load();
    http.expectOne('/api/workspaces').flush([workspace('a', 'Acme'), workspace('b', 'Beta')]);
    await loading;

    store.select('b');

    // A fresh store reads the same browser storage, which is what a page reload does.
    const fresh = TestBed.inject(WorkspaceStore);
    const reloading = fresh.load();
    http.expectOne('/api/workspaces').flush([workspace('a', 'Acme'), workspace('b', 'Beta')]);
    await reloading;

    expect(fresh.currentId()).toBe('b');
  });

  /**
   * The case that matters: someone is removed from the workspace their browser remembers.
   */
  it('falls back when the remembered workspace is no longer visible', async () => {
    localStorage.setItem('workboard.workspaceId', 'removed-from-this-one');

    const loading = store.load();
    http.expectOne('/api/workspaces').flush([workspace('a', 'Acme')]);
    await loading;

    expect(store.currentId()).toBe('a');
  });

  it('reports admin rights from the role on the membership', async () => {
    const admin = { ...workspace('a', 'Acme'), myRole: 'Admin' as const };

    const loading = store.load();
    http.expectOne('/api/workspaces').flush([admin]);
    await loading;

    expect(store.isAdmin()).toBe(true);
  });

  it('does not report admin rights for a plain member', async () => {
    const loading = store.load();
    http.expectOne('/api/workspaces').flush([workspace('a', 'Acme')]);
    await loading;

    expect(store.isAdmin()).toBe(false);
  });

  it('throws rather than silently building a URL with no workspace', () => {
    expect(() => store.requireId()).toThrow();
  });
});
