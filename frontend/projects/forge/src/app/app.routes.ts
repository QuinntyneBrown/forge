import { Routes } from '@angular/router';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { SignInPage } from './pages/sign-in/sign-in.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sign-in' },
  { path: 'sign-in', component: SignInPage },
  { path: 'dashboard', component: DashboardPage }
];
