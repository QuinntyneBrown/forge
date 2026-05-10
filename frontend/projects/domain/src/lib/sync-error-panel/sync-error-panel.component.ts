import { Component, Inject, Input, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HEALTH_SERVICE, IHealthService } from 'api';
import {
  BadgeComponent,
  BadgeVariant,
  ButtonComponent,
  CardComponent,
  ErrorBannerComponent
} from 'components';

@Component({
  selector: 'forge-sync-error-panel',
  imports: [CardComponent, ErrorBannerComponent, BadgeComponent, ButtonComponent],
  templateUrl: './sync-error-panel.component.html',
  styleUrl: './sync-error-panel.component.scss'
})
export class SyncErrorPanelComponent implements OnInit {
  @Input() traceId: string | null = null;

  protected readonly healthStatus = signal<string>('Loading…');
  protected readonly healthVariant = signal<BadgeVariant>('neutral');
  protected readonly healthkitStatus = signal<string>('Deferred');

  constructor(
    @Inject(HEALTH_SERVICE) private readonly health: IHealthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.health.getStatus().subscribe({
      next: (result) => {
        this.healthStatus.set(result.status);
        this.healthVariant.set(result.status === 'Healthy' ? 'success' : 'error');
      },
      error: () => {
        this.healthStatus.set('Unreachable');
        this.healthVariant.set('error');
      }
    });
  }

  protected goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }
}
