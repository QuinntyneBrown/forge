import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AppShellComponent, NavDestination } from 'components';
import { ProfileFormComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

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
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);

  protected readonly destinations = PRIMARY_DESTINATIONS;

  protected onDeleted(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }
}
