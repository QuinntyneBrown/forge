import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { Tier } from './models/tier.model';

export interface IRewardsService {
  getCurrentTier(): Observable<Tier>;
}

export const REWARDS_SERVICE = new InjectionToken<IRewardsService>('IRewardsService');
