import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { IProfileService } from './profile.service.contract';
import { UpdateProfileRequest } from './models/update-profile-request.model';

@Injectable()
export class ProfileService implements IProfileService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/profile`, request);
  }
}
