import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { UpdateProfileRequest } from './models/update-profile-request.model';

export interface UpdateMorningWindowRequest {
  start: string; // 'HH:mm'
  end: string;
  reminderEnabled: boolean;
}

export interface UpdateKitchenWindowRequest {
  start: string; // 'HH:mm'
  end: string;
  nudgeEnabled: boolean;
}

export interface IProfileService {
  updateProfile(request: UpdateProfileRequest): Observable<void>;
  updateMorningWindow(request: UpdateMorningWindowRequest): Observable<void>;
  updateKitchenWindow(request: UpdateKitchenWindowRequest): Observable<void>;
  setLeaderboardOptIn(leaderboardOptIn: boolean): Observable<void>;
  setWeightGoal(monthlyWeightGoalLb: number): Observable<void>;
}

export const PROFILE_SERVICE = new InjectionToken<IProfileService>('IProfileService');
