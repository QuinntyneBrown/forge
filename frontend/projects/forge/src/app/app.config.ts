import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import {
  API_BASE_URL,
  AUTH_SERVICE,
  AuthService,
  DASHBOARD_SERVICE,
  DashboardService,
  HEALTH_SERVICE,
  HealthService,
  ME_SERVICE,
  MeService,
  PROFILE_SERVICE,
  ProfileService,
  REWARDS_SERVICE,
  RewardsService,
  LEADERBOARD_SERVICE,
  LeaderboardService
} from 'api';
import { AuthStateService } from './auth-state.service';
import { authInterceptor } from './auth.interceptor';
import { refreshInterceptor } from './refresh.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimationsAsync(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, refreshInterceptor])),
    { provide: API_BASE_URL, useValue: 'https://localhost:5001' },
    { provide: AUTH_SERVICE, useClass: AuthService },
    { provide: HEALTH_SERVICE, useClass: HealthService },
    { provide: ME_SERVICE, useClass: MeService },
    { provide: PROFILE_SERVICE, useClass: ProfileService },
    { provide: DASHBOARD_SERVICE, useClass: DashboardService },
    { provide: REWARDS_SERVICE, useClass: RewardsService },
    { provide: LEADERBOARD_SERVICE, useClass: LeaderboardService },
    provideAppInitializer(() => inject(AuthStateService).tryHydrate())
  ]
};
