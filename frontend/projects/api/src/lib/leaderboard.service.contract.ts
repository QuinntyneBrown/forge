import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { LeaderboardEntry } from './models/leaderboard-entry.model';

export interface ILeaderboardService {
  list(page?: number, pageSize?: number): Observable<LeaderboardEntry[]>;
}

export const LEADERBOARD_SERVICE = new InjectionToken<ILeaderboardService>('ILeaderboardService');
