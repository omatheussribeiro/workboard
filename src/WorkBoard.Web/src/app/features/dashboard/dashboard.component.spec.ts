import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DashboardApi } from '../../core/api/dashboard-api.service';
import type { DashboardView } from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { DashboardComponent } from './dashboard.component';

class FakeDashboardApi {
  calls = 0;

  get(): Promise<DashboardView> {
    this.calls += 1;
    return Promise.resolve({
      totals: { projects: 0, tasks: 0, openTasks: 0, overdueTasks: 0, members: 1 },
      byStatus: [],
      byPriority: [],
      projects: [],
      throughput: [],
      myOpenTasks: [],
    });
  }
}

async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  fixture.detectChanges();
  await fixture.whenStable();
}

describe('DashboardComponent', () => {
  function render(workspaceId: string | null) {
    const api = new FakeDashboardApi();

    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: DashboardApi, useValue: api },
        { provide: ToastService, useValue: { fromError: vi.fn() } },
        { provide: WorkspaceStore, useValue: { currentId: () => workspaceId } },
      ],
    });

    return { fixture: TestBed.createComponent(DashboardComponent), api };
  }

  it('shows first-run onboarding without calling a scoped endpoint', async () => {
    const { fixture, api } = render(null);

    fixture.detectChanges();
    await settle(fixture);

    expect(api.calls).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Welcome to WorkBoard');
    expect(fixture.nativeElement.querySelector('a[href="/workspaces/new"]')).not.toBeNull();
  });

  it('loads dashboard data after a workspace is selected', async () => {
    const { fixture, api } = render('ws-1');

    fixture.detectChanges();
    await settle(fixture);

    expect(api.calls).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Everything in this workspace');
  });
});
