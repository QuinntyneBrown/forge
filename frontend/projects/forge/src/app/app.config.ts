import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import {
  API_BASE_URL,
  AUTH_SERVICE,
  AuthService,
  HEALTH_SERVICE,
  HealthService
} from 'api';
import { authInterceptor } from './auth.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimationsAsync(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: API_BASE_URL, useValue: 'https://localhost:5001' },
    { provide: AUTH_SERVICE, useClass: AuthService },
    { provide: HEALTH_SERVICE, useClass: HealthService }
  ]
};
