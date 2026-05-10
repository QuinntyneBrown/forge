import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { HEALTH_SERVICE, IHealthService } from 'api';
import { BadgeComponent, BadgeVariant, CardComponent } from 'components';

@Component({
  selector: 'forge-health-badge',
  imports: [CardComponent, BadgeComponent],
  templateUrl: './health-badge.component.html',
  styleUrl: './health-badge.component.scss'
})
export class HealthBadgeComponent implements OnInit {
  protected readonly status = signal<string>('Loading…');
  protected readonly isHealthy = signal<boolean>(false);
  protected readonly badgeVariant = computed<BadgeVariant>(() =>
    this.isHealthy() ? 'success' : 'error'
  );

  constructor(@Inject(HEALTH_SERVICE) private readonly health: IHealthService) {}

  ngOnInit(): void {
    this.health.getStatus().subscribe({
      next: (result) => {
        this.status.set(result.status);
        this.isHealthy.set(result.status === 'Healthy');
      },
      error: () => {
        this.status.set('Unreachable');
        this.isHealthy.set(false);
      }
    });
  }
}
