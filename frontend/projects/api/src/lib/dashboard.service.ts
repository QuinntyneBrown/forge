import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { IDashboardService } from './dashboard.service.contract';
import { DashboardSummary } from './models/dashboard-summary.model';

@Injectable()
export class DashboardService implements IDashboardService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/api/dashboard`);
  }
}
