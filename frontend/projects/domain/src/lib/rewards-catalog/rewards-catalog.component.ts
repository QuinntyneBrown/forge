import { Component, Inject, OnInit, computed, signal } from '@angular/core';
import {
  DASHBOARD_SERVICE,
  IDashboardService,
  IRewardsService,
  REWARDS_SERVICE,
  Reward
} from 'api';
import { ButtonComponent, CardComponent } from 'components';

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return hash;
}

@Component({
  selector: 'forge-rewards-catalog',
  imports: [CardComponent, ButtonComponent],
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

  protected iconFor(reward: Reward): string {
    const name = (reward.name + ' ' + reward.description).toLowerCase();
    if (/tv|stream|movie|show|netflix/.test(name)) return 'tv';
    if (/spa|massage|relax/.test(name)) return 'spa';
    if (/headphone|music|audio/.test(name)) return 'headphones';
    if (/gift|card/.test(name)) return 'card_giftcard';
    if (/restaurant|meal|food|dinner|lunch/.test(name)) return 'restaurant';
    if (/coffee|cafe/.test(name)) return 'local_cafe';
    if (/book|read/.test(name)) return 'menu_book';
    return 'redeem';
  }

  protected toneFor(reward: Reward): 'teal' | 'orange' | 'blue' | 'gold' {
    const tones: ('teal' | 'orange' | 'blue' | 'gold')[] = ['teal', 'orange', 'blue', 'gold'];
    const idx = Math.abs(hashString(reward.id)) % tones.length;
    return tones[idx];
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
