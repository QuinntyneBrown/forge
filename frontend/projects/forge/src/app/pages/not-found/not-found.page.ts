import { Component, inject } from '@angular/core';
import { Location } from '@angular/common';
import { Router } from '@angular/router';
import { AppShellComponent, NavDestination } from 'components';

const DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-not-found-page',
  imports: [AppShellComponent],
  templateUrl: './not-found.page.html',
  styleUrl: './not-found.page.scss'
})
export class NotFoundPage {
  private readonly router = inject(Router);
  private readonly location = inject(Location);

  protected readonly destinations = DESTINATIONS;

  protected goHome(): void {
    this.router.navigate(['/dashboard']);
  }

  protected goBack(): void {
    this.location.back();
  }
}
