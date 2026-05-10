import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
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
  private readonly queryParams = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap
  });

  protected readonly traceId = computed(() => this.queryParams().get('traceId'));

  /** Driven by ?code=... query param. Falls back to the mock's default. */
  protected readonly errorCodeLabel = computed(() => {
    const raw = (this.queryParams().get('code') ?? '').trim();
    if (!raw) {
      return 'ERR_HEALTHKIT_OFFLINE · 0xA3';
    }
    // Numeric HTTP-style codes get an "Error " prefix; anything else
    // (already an identifier like ERR_FOO) is rendered verbatim.
    return /^\d+$/.test(raw) ? `Error ${raw}` : raw;
  });

  protected onRetry(): void {
    if (typeof window !== 'undefined') {
      window.location.reload();
    }
  }

  protected readonly diagnostics: DiagnosticRow[] = [
    { label: 'Forge Fit servers', detail: 'Online · 32 ms', icon: 'check_circle', tone: 'ok' },
    { label: 'Internet', detail: 'Reachable · WiFi', icon: 'check_circle', tone: 'ok' },
    { label: 'HealthKit authorization', detail: 'Permission needed for workouts', icon: 'warning', tone: 'warn' },
    { label: 'Apple Watch reachable', detail: 'Last seen 14 min ago', icon: 'schedule', tone: 'idle' }
  ];
}
