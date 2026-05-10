import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_SERVICE, CurrentUser, IAuthService, IMeService, ME_SERVICE } from 'api';
import { AppShellComponent, NavDestination } from 'components';
import { HealthBadgeComponent } from 'domain';
import { AuthStateService } from '../../auth-state.service';

const PRIMARY_DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

@Component({
  selector: 'app-dashboard-page',
  imports: [AppShellComponent, HealthBadgeComponent],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss'
})
export class DashboardPage implements OnInit {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);

  protected readonly destinations = PRIMARY_DESTINATIONS;
  protected readonly currentUser = signal<CurrentUser | null>(null);
  protected readonly email = computed(
    () => this.currentUser()?.email ?? this.auth.snapshot()?.email ?? 'unknown'
  );
  protected readonly role = computed(
    () => this.currentUser()?.role ?? this.auth.snapshot()?.role ?? 'unknown'
  );

  constructor(
    @Inject(AUTH_SERVICE) private readonly authApi: IAuthService,
    @Inject(ME_SERVICE) private readonly meApi: IMeService
  ) {}

  ngOnInit(): void {
    this.meApi.getMe().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => {
        // Auth interceptor handles 401 (refresh+retry or sign-out). Anything
        // else falls back silently to AuthStateService snapshot via the
        // computed defaults so the dashboard isn't blank on transient errors.
      }
    });
  }

  protected signOut(): void {
    const refreshToken = this.auth.refreshToken;
    const finalize = (): void => {
      this.auth.clear();
      this.router.navigate(['/sign-in']);
    };

    if (!refreshToken) {
      finalize();
      return;
    }

    this.authApi.signOut(refreshToken).subscribe({
      next: () => finalize(),
      error: () => finalize()
    });
  }
}
