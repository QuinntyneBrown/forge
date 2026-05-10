import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_SERVICE, CurrentUser, IAuthService, IMeService, ME_SERVICE } from 'api';
import { AppShellComponent, NavDestination } from 'components';
import {
  DailyRingCardComponent,
  LeaderboardCardComponent,
  StreakCardComponent,
  TierCardComponent,
  WeightProgressCardComponent
} from 'domain';
import { AuthStateService } from '../../auth-state.service';

const PRIMARY_DESTINATIONS: NavDestination[] = [
  { label: 'Home', icon: 'home', routerLink: '/dashboard' },
  { label: 'Workouts', icon: 'fitness_center', routerLink: '/workouts' },
  { label: 'Rewards', icon: 'redeem', routerLink: '/rewards' },
  { label: 'Profile', icon: 'person', routerLink: '/profile' }
];

const DATE_FORMAT: Intl.DateTimeFormatOptions = {
  weekday: 'long',
  month: 'long',
  day: 'numeric'
};

@Component({
  selector: 'app-dashboard-page',
  imports: [
    AppShellComponent,
    DailyRingCardComponent,
    StreakCardComponent,
    WeightProgressCardComponent,
    TierCardComponent,
    LeaderboardCardComponent
  ],
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
  protected readonly firstName = computed(() => this.currentUser()?.firstName ?? '');
  protected readonly userInitial = computed(() => {
    const name = this.firstName();
    if (name.length > 0) {
      return name.charAt(0).toUpperCase();
    }
    const email = this.email();
    return email.length > 0 ? email.charAt(0).toUpperCase() : '?';
  });

  protected readonly greeting = signal(this.computeGreeting());
  protected readonly today = signal(this.computeTodayLabel());

  constructor(
    @Inject(AUTH_SERVICE) private readonly authApi: IAuthService,
    @Inject(ME_SERVICE) private readonly meApi: IMeService
  ) {}

  ngOnInit(): void {
    this.meApi.getMe().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => undefined
    });
  }

  protected logWorkout(): void {
    this.router.navigate(['/workouts', 'new']);
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

  private computeGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) {
      return 'Good morning';
    }
    if (hour < 18) {
      return 'Good afternoon';
    }
    return 'Good evening';
  }

  private computeTodayLabel(): string {
    return new Date().toLocaleDateString(undefined, DATE_FORMAT);
  }
}
