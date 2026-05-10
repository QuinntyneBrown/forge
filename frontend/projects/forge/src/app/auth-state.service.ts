import { Injectable, signal } from '@angular/core';
import { AuthResult } from 'api';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
  private readonly current = signal<AuthResult | null>(null);

  readonly snapshot = this.current.asReadonly();

  setSession(result: AuthResult): void {
    this.current.set(result);
  }

  clear(): void {
    this.current.set(null);
  }

  get token(): string | null {
    return this.current()?.accessToken ?? null;
  }

  get refreshToken(): string | null {
    return this.current()?.refreshToken ?? null;
  }
}
