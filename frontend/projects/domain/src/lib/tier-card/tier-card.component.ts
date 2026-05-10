import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import {
  DASHBOARD_SERVICE,
  DashboardSummary,
  IDashboardService,
  IRewardsService,
  REWARDS_SERVICE,
  Tier
} from 'api';
import { CardComponent } from 'components';

@Component({
  selector: 'forge-tier-card',
  imports: [CardComponent],
  templateUrl: './tier-card.component.html',
  styleUrl: './tier-card.component.scss'
})
export class TierCardComponent implements OnInit {
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly tier = signal<Tier | null>(null);

  protected readonly tierName = computed(() => this.tier()?.name ?? '—');
  protected readonly currentBalance = computed(() => this.summary()?.currentBalance ?? 0);
  protected readonly progressLabel = computed(() => {
    const t = this.tier();
    if (!t || t.nextTierName === null || t.pointsToNextTier === null) {
      return 'Top tier reached';
    }
    return `${t.pointsToNextTier} pts to ${t.nextTierName}`;
  });

  constructor(
    @Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService,
    @Inject(REWARDS_SERVICE) private readonly rewards: IRewardsService
  ) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (result) => this.summary.set(result),
      error: () => undefined
    });
    this.rewards.getCurrentTier().subscribe({
      next: (result) => this.tier.set(result),
      error: () => undefined
    });
  }
}
