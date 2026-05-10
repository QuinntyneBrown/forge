import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { DashboardSummary } from './models/dashboard-summary.model';

export interface IDashboardService {
  getSummary(): Observable<DashboardSummary>;
}

export const DASHBOARD_SERVICE = new InjectionToken<IDashboardService>('IDashboardService');
