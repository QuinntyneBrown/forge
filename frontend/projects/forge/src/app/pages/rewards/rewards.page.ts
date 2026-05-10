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

  // Static catalog mirrors docs/mocks/rewards.html. The backend doesn't yet
  // expose user achievements / in-flight goals, so the page renders the mock
  // copy as a structural scaffold ready for real data.
  protected readonly achievements = [
    { tone: 'gold', icon: 'wb_sunny', count: 7, title: 'Morning Warrior', sub: 'Workout before 7 AM' },
    { tone: 'teal', icon: 'local_fire_department', count: 3, title: '1500-Cal Club', sub: 'Hit daily calorie goal' },
    { tone: 'blue', icon: 'nightlight', count: 5, title: 'Night Resister', sub: 'No food after 8 PM' },
    { tone: 'orange', icon: 'emoji_events', count: 1, title: 'Iron Week', sub: 'All 4 machines this week' },
    { tone: 'teal', icon: 'timer', count: null as number | null, title: '300-Min Week', sub: '5 hours moved' },
    { tone: 'gold', icon: 'monitor_weight', count: null, title: 'First 5 lb Down', sub: 'Trend confirmed' }
  ];

  protected readonly inFlight = [
    { tone: 'teal', icon: 'wb_sunny', title: 'Morning Warrior x10', sub: '10 morning workouts in 14 days', current: 7, target: 10 },
    { tone: 'orange', icon: 'monitor_weight', title: '-20 lb May', sub: 'Monthly weight goal — trend confirmed', current: 5.2, target: 20 },
    { tone: 'blue', icon: 'nightlight', title: 'Night Resister x10', sub: '10 fasted nights in a row', current: 5, target: 10 },
    { tone: 'teal', icon: 'local_fire_department', title: '1500-Cal Club ×7', sub: 'Hit 1,500 active cal seven days in a row', current: 3, target: 7 }
  ];

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
