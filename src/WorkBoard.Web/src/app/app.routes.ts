import type { Routes } from '@angular/router';
import { anonymousGuard, authGuard } from './core/auth/auth.guard';

/**
 * Every feature is loaded lazily.
 *
 * The initial bundle then contains the shell, the router and the auth code — which is all a visitor
 * on the login page needs. The board pulls in the CDK drag-and-drop machinery, and there is no reason
 * to make someone signing in download it.
 *
 * The auth screens sit outside the shell on purpose: they must render without a workspace, and the
 * shell's first act is to load one.
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [anonymousGuard],
    title: 'Sign in · WorkBoard',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [anonymousGuard],
    title: 'Create an account · WorkBoard',
    loadComponent: () =>
      import('./features/auth/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · WorkBoard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'projects',
        title: 'Projects · WorkBoard',
        loadComponent: () =>
          import('./features/projects/projects.component').then((m) => m.ProjectsComponent),
      },
      {
        path: 'boards/:boardId',
        title: 'Board · WorkBoard',
        loadComponent: () =>
          import('./features/boards/board.component').then((m) => m.BoardComponent),
      },
      {
        path: 'tasks',
        title: 'Tasks · WorkBoard',
        loadComponent: () =>
          import('./features/tasks/tasks.component').then((m) => m.TasksComponent),
      },
      {
        path: 'tasks/:taskId',
        title: 'Task · WorkBoard',
        loadComponent: () =>
          import('./features/tasks/task-detail.component').then((m) => m.TaskDetailComponent),
      },
      {
        path: 'workspaces/new',
        title: 'New workspace · WorkBoard',
        loadComponent: () =>
          import('./features/workspaces/create-workspace.component').then(
            (m) => m.CreateWorkspaceComponent,
          ),
      },
      {
        path: 'activity',
        title: 'Activity · WorkBoard',
        loadComponent: () =>
          import('./features/activity/activity.component').then((m) => m.ActivityComponent),
      },
      {
        path: 'notifications',
        title: 'Notifications · WorkBoard',
        loadComponent: () =>
          import('./features/notifications/notifications.component').then(
            (m) => m.NotificationsComponent,
          ),
      },
      {
        path: 'search',
        title: 'Search · WorkBoard',
        loadComponent: () =>
          import('./features/search/search.component').then((m) => m.SearchComponent),
      },
      {
        path: 'settings',
        title: 'Settings · WorkBoard',
        loadComponent: () =>
          import('./features/settings/settings.component').then((m) => m.SettingsComponent),
      },
    ],
  },

  // An unknown path goes home rather than to a 404 screen. Every route here is reached from inside
  // the app, so a bad URL is almost always a stale link rather than a typo worth a dedicated page.
  { path: '**', redirectTo: '' },
];
