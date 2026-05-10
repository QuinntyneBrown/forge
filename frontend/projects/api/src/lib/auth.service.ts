import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { IAuthService } from './auth.service.contract';
import { AuthResult } from './models/auth-result.model';
import { SignInRequest } from './models/sign-in-request.model';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

@Injectable()
export class AuthService implements IAuthService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  signIn(request: SignInRequest): Observable<AuthResult> {
    return this.http.post<AuthResult>(`${this.baseUrl}/api/auth/sign-in`, request);
  }
}
