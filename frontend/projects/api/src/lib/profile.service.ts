import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import {
  IProfileService,
  UpdateKitchenWindowRequest,
  UpdateMorningWindowRequest
} from './profile.service.contract';
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

  updateMorningWindow(request: UpdateMorningWindowRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/profile/morning-window`, request);
  }

  updateKitchenWindow(request: UpdateKitchenWindowRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/profile/kitchen-window`, request);
  }

  setLeaderboardOptIn(leaderboardOptIn: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/profile/leaderboard-opt-in`, {
      leaderboardOptIn
    });
  }

  setWeightGoal(monthlyWeightGoalLb: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/profile/weight-goal`, {
      monthlyWeightGoalLb
    });
  }
}
