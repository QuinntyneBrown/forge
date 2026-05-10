import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthStateService } from './auth-state.service';

export const authGuard: CanActivateFn = (_route, state): boolean | UrlTree => {
  const auth = inject(AuthStateService);
  const router = inject(Router);

  if (auth.snapshot()) {
    return true;
  }

  return router.createUrlTree(['/sign-in'], { queryParams: { returnUrl: state.url } });
};
