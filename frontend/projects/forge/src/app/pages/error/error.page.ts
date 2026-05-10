import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

interface DiagnosticRow {
  label: string;
  detail: string;
  icon: string;
  tone: 'ok' | 'warn' | 'idle' | 'error';
}

@Component({
  selector: 'app-error-page',
  imports: [RouterLink],
  templateUrl: './error.page.html',
  styleUrl: './error.page.scss'
})
export class ErrorPage {
  private readonly route = inject(ActivatedRoute);
  protected readonly traceId = this.route.snapshot.queryParamMap.get('traceId');
  protected readonly errorCode =
    this.route.snapshot.queryParamMap.get('code') ?? 'ERR_HEALTHKIT_OFFLINE · 0xA3';

  protected readonly diagnostics: DiagnosticRow[] = [
    { label: 'Forge Fit servers', detail: 'Online · 32 ms', icon: 'check_circle', tone: 'ok' },
    { label: 'Internet', detail: 'Reachable · WiFi', icon: 'check_circle', tone: 'ok' },
    { label: 'HealthKit authorization', detail: 'Permission needed for workouts', icon: 'warning', tone: 'warn' },
    { label: 'Apple Watch reachable', detail: 'Last seen 14 min ago', icon: 'schedule', tone: 'idle' }
  ];
}
