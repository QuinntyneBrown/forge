import { Component } from '@angular/core';
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
  imports: [AppShellComponent, TierCardComponent, RewardsCatalogComponent],
  templateUrl: './rewards.page.html',
  styleUrl: './rewards.page.scss'
})
export class RewardsPage {
  protected readonly destinations = DESTINATIONS;
}
