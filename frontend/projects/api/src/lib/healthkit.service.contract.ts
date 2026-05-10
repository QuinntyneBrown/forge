import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

export interface HealthKitSample {
  sampleType: string;
  value: number;
  unit: string;
  recordedAt: string;
}

export interface IHealthKitService {
  ingest(sample: HealthKitSample): Observable<void>;
}

export const HEALTHKIT_SERVICE = new InjectionToken<IHealthKitService>('IHealthKitService');
