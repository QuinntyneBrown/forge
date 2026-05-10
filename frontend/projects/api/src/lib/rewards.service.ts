import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { IRewardsService } from './rewards.service.contract';
import { Tier } from './models/tier.model';

@Injectable()
export class RewardsService implements IRewardsService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  getCurrentTier(): Observable<Tier> {
    return this.http.get<Tier>(`${this.baseUrl}/api/tier`);
  }
}
