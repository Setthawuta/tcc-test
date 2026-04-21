import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './shared/layouts/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'login' },
      {
        path: 'login',
        data: { title: 'IT 02-1' },
        loadComponent: () =>
          import('./features/login/login.component').then((m) => m.LoginComponent),
      },
      {
        path: 'register',
        data: { title: 'IT 02-2' },
        loadComponent: () =>
          import('./features/register/register.component').then((m) => m.RegisterComponent),
      },
      {
        path: 'welcome',
        data: { title: 'IT 02-3' },
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/welcome/welcome.component').then((m) => m.WelcomeComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
