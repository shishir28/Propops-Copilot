import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';
import { roleGuard } from './guards/role.guard';
import { REQUEST_CREATOR_ROLES, STAFF_ROLES } from './core/portal-roles';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'workspace'
  },
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login-page.component').then((m) => m.LoginPageComponent)
  },
  {
    path: 'workspace',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/workspace-page.component').then((m) => m.WorkspacePageComponent)
  },
  {
    path: 'overview',
    canActivate: [authGuard, roleGuard(STAFF_ROLES)],
    loadComponent: () =>
      import('./pages/overview-page.component').then((m) => m.OverviewPageComponent)
  },
  {
    path: 'intake',
    canActivate: [authGuard, roleGuard(REQUEST_CREATOR_ROLES)],
    loadComponent: () =>
      import('./pages/intake-page.component').then((m) => m.IntakePageComponent)
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
