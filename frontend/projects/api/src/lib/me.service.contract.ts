import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { CurrentUser } from './models/current-user.model';

export interface IMeService {
  getMe(): Observable<CurrentUser>;
  deleteMe(): Observable<void>;
}

export const ME_SERVICE = new InjectionToken<IMeService>('IMeService');
