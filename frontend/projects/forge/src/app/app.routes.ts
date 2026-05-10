import { Routes } from '@angular/router';
import { authGuard } from './auth.guard';
import { DashboardPage } from './pages/dashboard/dashboard.page';
import { PasswordResetPage } from './pages/password-reset/password-reset.page';
import { ProfilePage } from './pages/profile/profile.page';
import { SignInPage } from './pages/sign-in/sign-in.page';
import { SignUpPage } from './pages/sign-up/sign-up.page';
import { WorkoutsPage } from './pages/workouts/workouts.page';
import { WorkoutNewPage } from './pages/workout-new/workout-new.page';
import { WorkoutDetailPage } from './pages/workout-detail/workout-detail.page';
import { RewardsPage } from './pages/rewards/rewards.page';
import { ErrorPage } from './pages/error/error.page';
import { NotFoundPage } from './pages/not-found/not-found.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sign-in' },
  { path: 'sign-in', component: SignInPage },
  { path: 'sign-up', component: SignUpPage },
  { path: 'password-reset', component: PasswordResetPage },
  { path: 'dashboard', component: DashboardPage, canActivate: [authGuard] },
  { path: 'profile', component: ProfilePage, canActivate: [authGuard] },
  { path: 'workouts', component: WorkoutsPage, canActivate: [authGuard] },
  { path: 'workouts/new', component: WorkoutNewPage, canActivate: [authGuard] },
  { path: 'workouts/:id', component: WorkoutDetailPage, canActivate: [authGuard] },
  { path: 'rewards', component: RewardsPage, canActivate: [authGuard] },
  { path: 'error', component: ErrorPage },
  { path: '**', component: NotFoundPage }
];
