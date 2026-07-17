import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  {
    path: 'signin',
    loadComponent: () =>
      import('./features/auth/signin-page.component').then((m) => m.SigninPageComponent),
    title: 'Sign In',
  },
  {
    path: 'signup',
    loadComponent: () =>
      import('./features/auth/signup-page.component').then((m) => m.SignupPageComponent),
    title: 'Sign Up',
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            (m) => m.DashboardPageComponent,
          ),
        title: 'Dashboard',
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/projects/projects-page.component').then(
            (m) => m.ProjectsPageComponent,
          ),
        title: 'Projects',
      },
      {
        path: 'backlog',
        loadComponent: () =>
          import('./features/backlog/backlog-page.component').then((m) => m.BacklogPageComponent),
        title: 'Backlog',
      },
      {
        path: 'sprints',
        loadComponent: () =>
          import('./features/sprints/sprint-page.component').then((m) => m.SprintPageComponent),
        title: 'Sprints',
      },
      {
        path: 'team',
        loadComponent: () =>
          import('./features/team/team-page.component').then((m) => m.TeamPageComponent),
        title: 'Team',
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/profile-page.component').then((m) => m.ProfilePageComponent),
        title: 'Profile',
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/not-found-page.component').then((m) => m.NotFoundPageComponent),
    title: 'Page Not Found',
  },
];
