import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { HealthStatus } from './models/health-status.model';

export interface IHealthService {
  getStatus(): Observable<HealthStatus>;
}

export const HEALTH_SERVICE = new InjectionToken<IHealthService>('IHealthService');
