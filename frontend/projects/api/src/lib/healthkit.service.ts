import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';
import { HealthKitSample, IHealthKitService } from './healthkit.service.contract';

@Injectable()
export class HealthKitService implements IHealthKitService {
  constructor(
    private readonly http: HttpClient,
    @Inject(API_BASE_URL) private readonly baseUrl: string
  ) {}

  ingest(sample: HealthKitSample): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/api/healthkit/ingest`, sample);
  }
}
