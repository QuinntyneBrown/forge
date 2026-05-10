import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { ILeaderboardService } from './leaderboard.service.contract';
import { LeaderboardEntry } from './models/leaderboard-entry.model';

@Injectable()
export class LeaderboardService implements ILeaderboardService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  list(page = 1, pageSize = 25): Observable<LeaderboardEntry[]> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<LeaderboardEntry[]>(`${this.baseUrl}/api/leaderboard`, { params });
  }
}
