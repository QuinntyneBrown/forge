import { Inject, Injectable, signal } from '@angular/core';
import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';
import { firstValueFrom } from 'rxjs';

const PERSISTED_REFRESH_TOKEN_KEY = 'forge.auth.refreshToken';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly current = signal<AuthResult | null>(null);

  readonly snapshot = this.current.asReadonly();

  constructor(@Inject(AUTH_SERVICE) private readonly authService: IAuthService) {}

  setSession(result: AuthResult, persist = false): void {
    this.current.set(result);
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
  }

  get token(): string | null {
    return this.current()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.current()?.refreshToken ?? null;
  }

  async tryHydrate(): Promise<void> {
    let persisted: string | null = null;
    try {
      persisted = localStorage.getItem(PERSISTED_REFRESH_TOKEN_KEY);
    } catch {
      return;
    }
    if (!persisted) {
      return;
    }
    try {
      const rotated = await firstValueFrom(this.authService.refresh(persisted));
      this.setSession(rotated, true);
    } catch {
      // Refresh rejected — clear the stale entry.
      this.clear();
    }
  }
}
