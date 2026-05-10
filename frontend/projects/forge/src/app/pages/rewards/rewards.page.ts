import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import {
  DASHBOARD_SERVICE,
  DashboardSummary,
  IDashboardService,
  IRewardsService,
  REWARDS_SERVICE,
  Tier
} from 'api';
import { AppShellComponent, NavDestination } from 'components';
import { RewardsCatalogComponent, TierCardComponent } from 'domain';

const DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-rewards-page',
  imports: [CommonModule, AppShellComponent, TierCardComponent, RewardsCatalogComponent],
  templateUrl: './rewards.page.html',
  styleUrl: './rewards.page.scss'
})
export class RewardsPage implements OnInit {
  protected readonly destinations = DESTINATIONS;

  private readonly summary = signal<DashboardSummary | null>(null);
  private readonly tier = signal<Tier | null>(null);

  protected readonly balance = computed(() => this.summary()?.currentBalance ?? 0);
  protected readonly balanceSub = computed(() => {
    const lifetime = this.summary()?.lifetimePoints ?? 0;
    return `${lifetime.toLocaleString()} pts earned all-time`;
  });
  protected readonly tierName = computed(() => this.tier()?.name ?? 'Iron');
  protected readonly tierProgressLabel = computed(() => {
    const t = this.tier();
    if (!t || t.nextTierName === null || t.pointsToNextTier === null) {
      return 'Top tier reached';
    }
    return `${t.pointsToNextTier} pts to ${t.nextTierName}`;
  });
  protected readonly tierProgressPercent = computed(() => {
    const t = this.tier();
    const balance = this.balance();
    if (!t || t.pointsToNextTier === null) return 100;
    const total = balance + t.pointsToNextTier;
    if (total <= 0) return 0;
    return Math.min(100, Math.max(0, Math.round((balance / total) * 100)));
  });

  constructor(
    @Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService,
    @Inject(REWARDS_SERVICE) private readonly rewardsApi: IRewardsService
  ) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (result) => this.summary.set(result),
      error: () => undefined
    });
    this.rewardsApi.getCurrentTier().subscribe({
      next: (result) => this.tier.set(result),
      error: () => undefined
    });
  }
}
