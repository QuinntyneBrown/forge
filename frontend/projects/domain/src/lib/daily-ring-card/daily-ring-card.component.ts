import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { DASHBOARD_SERVICE, DashboardSummary, IDashboardService } from 'api';
import { CardComponent, ProgressRingComponent } from 'components';

@Component({
  selector: 'forge-daily-ring-card',
  imports: [CardComponent, ProgressRingComponent],
  templateUrl: './daily-ring-card.component.html',
  styleUrl: './daily-ring-card.component.scss'
})
export class DailyRingCardComponent implements OnInit {
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly errored = signal(false);

  protected readonly caloriesToday = computed(() => this.summary()?.caloriesToday ?? 0);
  protected readonly targetCalories = computed(() => this.summary()?.targetCalories ?? 1500);
  protected readonly minutesToday = computed(() => this.summary()?.minutesToday ?? 0);
  protected readonly targetMinutes = computed(() => this.summary()?.targetMinutes ?? 60);

  constructor(@Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (result) => this.summary.set(result),
      error: () => this.errored.set(true)
    });
  }
}
