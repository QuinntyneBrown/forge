import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { RedemptionResult, Reward } from './models/reward.model';
import { Tier } from './models/tier.model';

export interface IRewardsService {
  getCurrentTier(): Observable<Tier>;
  listRewards(): Observable<Reward[]>;
  redeem(rewardId: string): Observable<RedemptionResult>;
}

export const REWARDS_SERVICE = new InjectionToken<IRewardsService>('IRewardsService');
