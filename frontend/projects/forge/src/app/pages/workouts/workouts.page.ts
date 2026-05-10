import { Component } from '@angular/core';
import { AppShellComponent, NavDestination } from 'components';
import { WorkoutListComponent } from 'domain';

const DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-workouts-page',
  imports: [AppShellComponent, WorkoutListComponent],
  templateUrl: './workouts.page.html',
  styleUrl: './workouts.page.scss'
})
export class WorkoutsPage {
  protected readonly destinations = DESTINATIONS;
}
