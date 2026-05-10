import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import {
  DASHBOARD_SERVICE,
  IDashboardService,
  IRewardsService,
  REWARDS_SERVICE,
  Reward
} from 'api';
import { ButtonComponent, CardComponent, ProgressRingComponent } from 'components';

@Component({
  selector: 'forge-rewards-catalog',
  imports: [CardComponent, ButtonComponent, ProgressRingComponent],
  templateUrl: './rewards-catalog.component.html',
  styleUrl: './rewards-catalog.component.scss'
})
export class RewardsCatalogComponent implements OnInit {
  protected readonly rewards = signal<Reward[]>([]);
  protected readonly balance = signal(0);
  protected readonly busy = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  constructor(
    @Inject(REWARDS_SERVICE) private readonly rewardsApi: IRewardsService,
    @Inject(DASHBOARD_SERVICE) private readonly dashboard: IDashboardService
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  protected canAfford(reward: Reward): boolean {
    return this.balance() >= reward.costPoints;
  }

  protected progressToReward(reward: Reward): number {
    if (reward.costPoints <= 0) {
      return 100;
    }
    return Math.min(100, (this.balance() / reward.costPoints) * 100);
  }

  protected redeem(reward: Reward): void {
    if (!this.canAfford(reward) || this.busy() !== null) {
      return;
    }
    this.busy.set(reward.id);
    this.errorMessage.set(null);
    this.rewardsApi.redeem(reward.id).subscribe({
      next: (result) => {
        this.balance.set(result.remainingBalance);
        this.busy.set(null);
      },
      error: (err) => {
        this.busy.set(null);
        const title = err?.error?.title;
        if (title === 'INSUFFICIENT_POINTS') {
          this.errorMessage.set('You do not have enough points yet.');
        } else {
          this.errorMessage.set('Could not redeem reward.');
        }
      }
    });
  }

  private refresh(): void {
    this.rewardsApi.listRewards().subscribe({
      next: (rewards) => this.rewards.set(rewards),
      error: () => undefined
    });
    this.dashboard.getSummary().subscribe({
      next: (summary) => this.balance.set(summary.currentBalance),
      error: () => undefined
    });
  }
}
