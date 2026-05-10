import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthResult } from './models/auth-result.model';
import { SignInRequest } from './models/sign-in-request.model';

export interface IAuthService {
  signIn(request: SignInRequest): Observable<AuthResult>;
}

export const AUTH_SERVICE = new InjectionToken<IAuthService>('IAuthService');
