import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { DASHBOARD_SERVICE, DashboardSummary, IDashboardService } from 'api';
import { CardComponent } from 'components';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

@Component({
  selector: 'forge-weight-progress-card',
  imports: [CardComponent],
  templateUrl: './weight-progress-card.component.html',
  styleUrl: './weight-progress-card.component.scss'
})
export class WeightProgressCardComponent implements OnInit {
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly errored = signal(false);

  protected readonly goalLb = computed(() => this.summary()?.monthlyWeightGoalLb ?? 0);
  protected readonly mtdLossLb = computed(() => this.summary()?.monthToDateWeightLossLb ?? 0);
  protected readonly currentMonth = computed(() => {
    const now = new Date();
    return MONTH_NAMES[now.getMonth()];
  });

  protected readonly status = computed<'on-track' | 'behind' | 'no-goal'>(() => {
    const goal = this.goalLb();
    if (goal <= 0) {
      return 'no-goal';
    }
    const now = new Date();
    const dayOfMonth = now.getDate();
    const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
    const expected = (dayOfMonth / daysInMonth) * goal;
    return Number(this.mtdLossLb()) >= expected ? 'on-track' : 'behind';
  });

  protected readonly statusLabel = computed(() => {
    switch (this.status()) {
      case 'on-track':
        return 'On track';
      case 'behind':
        return 'Behind';
      default:
        return 'No goal set';
    }
  });

  protected readonly mtdLossLabel = computed(() => {
    const value = Number(this.mtdLossLb()).toFixed(1);
    return `−${value} lb so far in ${this.currentMonth()}`;
  });

  protected readonly goalLabel = computed(() => `−${this.goalLb()} lb / month`);

  constructor(@Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (result) => this.summary.set(result),
      error: () => this.errored.set(true)
    });
  }
}
