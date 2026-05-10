import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { UpdateProfileRequest } from './models/update-profile-request.model';

export interface IProfileService {
  updateProfile(request: UpdateProfileRequest): Observable<void>;
}

export const PROFILE_SERVICE = new InjectionToken<IProfileService>('IProfileService');
