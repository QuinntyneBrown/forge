import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import { DASHBOARD_SERVICE, DashboardSummary, IDashboardService } from 'api';
import { BadgeComponent, BadgeVariant, CardComponent } from 'components';

const STREAK_STEP = 0.01;
const STREAK_CAP = 1.5;

@Component({
  selector: 'forge-streak-card',
  imports: [CardComponent, BadgeComponent],
  templateUrl: './streak-card.component.html',
  styleUrl: './streak-card.component.scss'
})
export class StreakCardComponent implements OnInit {
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly errored = signal(false);

  protected readonly streakDays = computed(() => this.summary()?.currentStreak ?? 0);
  protected readonly multiplier = computed(() => {
    const days = this.streakDays();
    const computedValue = 1 + STREAK_STEP * days;
    return Math.min(STREAK_CAP, computedValue);
  });
  protected readonly multiplierLabel = computed(() => `×${this.multiplier().toFixed(2)}`);
  protected readonly badgeVariant = computed<BadgeVariant>(() =>
    this.streakDays() > 0 ? 'success' : 'neutral'
  );

  constructor(@Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (result) => this.summary.set(result),
      error: () => this.errored.set(true)
    });
  }
}
