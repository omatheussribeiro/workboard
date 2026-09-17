import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { BoardApi } from '../../core/api/board-api.service';
import { LabelApi } from '../../core/api/label-api.service';
import { ProjectApi } from '../../core/api/project-api.service';
import { TaskApi } from '../../core/api/task-api.service';
import type {
  BoardView,
  CreateTaskBody,
  PagedResult,
  ProjectSummary,
  TaskDetail,
  TaskListItem,
} from '../../core/models';
import { ToastService } from '../../core/notifications/toast.service';
import { TasksComponent } from './tasks.component';

const project: ProjectSummary = {
  id: 'project-1',
  name: 'WorkBoard',
  key: 'WB',
  description: null,
  isArchived: false,
  taskCount: 0,
  openTaskCount: 0,
  defaultBoardId: 'board-1',
  createdAtUtc: '2026-01-01T00:00:00Z',
};

const board: BoardView = {
  id: 'board-1',
  name: 'WorkBoard board',
  projectId: project.id,
  projectKey: project.key,
  columns: [
    {
      id: 'column-todo',
      name: 'To do',
      position: 1,
      category: 'Todo',
      wipLimit: null,
      taskCount: 0,
      cards: [],
    },
  ],
};

const createdTask: TaskDetail = {
  id: 'task-1',
  key: 'WB-1',
  projectId: project.id,
  projectName: project.name,
  boardColumnId: 'column-todo',
  columnName: 'To do',
  category: 'Todo',
  title: 'Ship task creation',
  description: null,
  priority: 'High',
  dueAtUtc: null,
  isOverdue: false,
  startedAtUtc: null,
  completedAtUtc: null,
  parentTaskId: null,
  parentTaskTitle: null,
  createdById: 'user-1',
  createdByName: 'Tester',
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
  assignees: [],
  labels: [],
  subtasks: [],
  rowVersion: 'version-1',
};

class FakeTaskApi {
  readonly created: CreateTaskBody[] = [];

  list(): Promise<PagedResult<TaskListItem>> {
    return Promise.resolve({
      items: [],
      page: 1,
      pageSize: 25,
      totalCount: 0,
      totalPages: 0,
      hasPreviousPage: false,
      hasNextPage: false,
    });
  }

  create(body: CreateTaskBody): Promise<TaskDetail> {
    this.created.push(body);
    return Promise.resolve({ ...createdTask, title: body.title, priority: body.priority });
  }
}

interface WritableValue<T> {
  (): T;
  set(value: T): void;
}

interface TasksInternals {
  newTaskTitle: WritableValue<string>;
  newTaskPriority: WritableValue<'Low' | 'Medium' | 'High' | 'Critical'>;
  showCreateForm: WritableValue<boolean>;
  createTask(): Promise<void>;
}

async function settle(fixture: ComponentFixture<unknown>): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve));
  fixture.detectChanges();
  await fixture.whenStable();
}

describe('TasksComponent task creation', () => {
  it('loads the project columns and posts a new task from the Tasks page', async () => {
    const taskApi = new FakeTaskApi();
    const boardApi = { get: vi.fn().mockResolvedValue(board) };
    const toasts = { success: vi.fn(), fromError: vi.fn() };

    TestBed.configureTestingModule({
      imports: [TasksComponent],
      providers: [
        provideRouter([]),
        { provide: TaskApi, useValue: taskApi },
        { provide: BoardApi, useValue: boardApi },
        { provide: ProjectApi, useValue: { list: () => Promise.resolve([project]) } },
        { provide: LabelApi, useValue: { list: () => Promise.resolve([]) } },
        { provide: ToastService, useValue: toasts },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => null } } },
        },
      ],
    });

    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(TasksComponent);
    fixture.detectChanges();
    await settle(fixture);

    const newTaskButton = [...fixture.nativeElement.querySelectorAll('button')].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'New task',
    ) as HTMLButtonElement;
    newTaskButton.click();
    await settle(fixture);

    const component = fixture.componentInstance as unknown as TasksInternals;
    expect(component.showCreateForm()).toBe(true);
    expect(boardApi.get).toHaveBeenCalledWith('board-1');

    component.newTaskTitle.set('Ship task creation');
    component.newTaskPriority.set('High');
    await component.createTask();

    expect(taskApi.created).toEqual([
      {
        projectId: 'project-1',
        boardColumnId: 'column-todo',
        title: 'Ship task creation',
        description: null,
        priority: 'High',
        dueAtUtc: null,
        parentTaskId: null,
        assigneeIds: null,
        labelIds: null,
      },
    ]);
    expect(toasts.success).toHaveBeenCalledWith('Created WB-1.');
    expect(navigate).toHaveBeenCalledWith(['/tasks', 'task-1']);
  });
});
