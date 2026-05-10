import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { IMeService } from './me.service.contract';
import { CurrentUser } from './models/current-user.model';

@Injectable()
export class MeService implements IMeService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  getMe(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.baseUrl}/api/me`);
  }

  deleteMe(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/me`);
  }
}
