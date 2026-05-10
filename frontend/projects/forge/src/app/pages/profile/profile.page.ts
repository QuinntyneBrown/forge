import { Component } from '@angular/core';
import { AppShellComponent, NavDestination } from 'components';
import { ProfileFormComponent } from 'domain';

const PRIMARY_DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-profile-page',
  imports: [AppShellComponent, ProfileFormComponent],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss'
})
export class ProfilePage {
  protected readonly destinations = PRIMARY_DESTINATIONS;
}
