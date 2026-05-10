import { Inject, Injectable, signal } from '@angular/core';
import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';
import { firstValueFrom } from 'rxjs';

const PERSISTED_REFRESH_TOKEN_KEY = 'forge.auth.refreshToken';
const SESSION_REFRESH_TOKEN_KEY = 'forge.auth.refreshToken.session';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly current = signal<AuthResult | null>(null);

  readonly snapshot = this.current.asReadonly();

  constructor(@Inject(AUTH_SERVICE) private readonly authService: IAuthService) {}

  setSession(result: AuthResult, persist = false): void {
    this.current.set(result);
    // Mirror the refresh token into sessionStorage so a same-tab navigation
    // (Playwright `page.goto`, manual reload) can re-hydrate via tryHydrate
    // without requiring Remember me. localStorage continues to scope to
    // Remember me — that's the cross-restart contract.
    try {
      sessionStorage.setItem(SESSION_REFRESH_TOKEN_KEY, result.refreshToken);
    } catch {
      // sessionStorage may be unavailable in some embedded contexts.
    }
    if (persist) {
      try {
        localStorage.setItem(PERSISTED_REFRESH_TOKEN_KEY, result.refreshToken);
      } catch {
        // Storage may be unavailable (private mode, quota); the in-memory
        // session continues without persistence.
      }
    }
  }

  clear(): void {
    this.current.set(null);
    try {
      localStorage.removeItem(PERSISTED_REFRESH_TOKEN_KEY);
    } catch {
      // ignore — same rationale as setSession.
    }
    try {
      sessionStorage.removeItem(SESSION_REFRESH_TOKEN_KEY);
    } catch {
      // ignore.
    }
  }

  get token(): string | null {
    return this.current()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.current()?.refreshToken ?? null;
  }

  async tryHydrate(): Promise<void> {
    // Prefer the persisted (Remember me) token over the session-scoped one
    // so a long-lived session resumes correctly across browser restarts.
    let token: string | null = null;
    let persistedHit = false;
    try {
      token = localStorage.getItem(PERSISTED_REFRESH_TOKEN_KEY);
      persistedHit = !!token;
    } catch {
      // ignore; fall through to sessionStorage.
    }
    if (!token) {
      try {
        token = sessionStorage.getItem(SESSION_REFRESH_TOKEN_KEY);
      } catch {
        return;
      }
    }
    if (!token) {
      return;
    }
    try {
      const rotated = await firstValueFrom(this.authService.refresh(token));
      this.setSession(rotated, persistedHit);
    } catch {
      // Refresh rejected — clear the stale entry.
      this.clear();
    }
  }
}
