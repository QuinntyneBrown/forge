import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_SERVICE, IAuthService } from 'api';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthStateService } from './auth-state.service';

const ALREADY_RETRIED = 'X-Forge-Refresh-Retried';

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStateService);
  const router = inject(Router);
  const authService = inject(AUTH_SERVICE) as IAuthService;

  // Don't try to refresh-and-retry the refresh / sign-in / register / sign-out
  // calls themselves — that would loop or undo the operation we're servicing.
  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }
      if (req.headers.has(ALREADY_RETRIED) || !auth.refreshToken) {
        auth.clear();
        router.navigate(['/sign-in']);
        return throwError(() => error);
      }
      return authService.refresh(auth.refreshToken).pipe(
        switchMap((rotated) => {
          auth.setSession(rotated);
          const retried = req.clone({
            setHeaders: {
              Authorization: `Bearer ${rotated.accessToken}`,
              [ALREADY_RETRIED]: 'true'
            }
          });
          return next(retried);
        }),
        catchError((refreshErr) => {
          auth.clear();
          router.navigate(['/sign-in']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};

function isAuthEndpoint(url: string): boolean {
  return /\/api\/auth\/(refresh|sign-in|register|sign-out)\b/.test(url);
}
