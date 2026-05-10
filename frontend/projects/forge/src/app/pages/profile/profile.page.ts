import { Component, Inject, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Router } from '@angular/router';
import { CurrentUser, IMeService, ME_SERVICE } from 'api';
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
  imports: [AppShellComponent, ProfileFormComponent, FormsModule, MatSlideToggleModule],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss'
})
export class ProfilePage implements OnInit {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly meApi = inject<IMeService>(ME_SERVICE);

  protected readonly destinations = PRIMARY_DESTINATIONS;
  protected readonly currentUser = signal<CurrentUser | null>(null);

  protected readonly goalCalories = signal<number | null>(null);
  protected readonly goalMinutes = signal<number | null>(null);
  protected readonly goalWeight = signal<number | null>(null);

  protected readonly morningStart = signal('');
  protected readonly morningEnd = signal('');
  protected readonly kitchenStart = signal('');
  protected readonly kitchenEnd = signal('');

  protected readonly morningReminder = signal(false);
  protected readonly kitchenNudge = signal(false);
  protected readonly leaderboard = signal(false);

  protected readonly displayName = computed(() => {
    const u = this.currentUser();
    if (u?.firstName || u?.lastName) {
      return `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
    }
    return u?.email?.split('@')[0] ?? 'You';
  });

  protected readonly email = computed(() =>
    this.currentUser()?.email ?? this.auth.snapshot()?.email ?? 'unknown'
  );

  protected readonly avatarInitials = computed(() => {
    const u = this.currentUser();
    const a = (u?.firstName ?? this.email().charAt(0)).charAt(0);
    const b = (u?.lastName ?? '').charAt(0);
    return `${a}${b}`.toUpperCase() || '?';
  });

  protected readonly memberSince = computed(() =>
    new Date().toLocaleDateString(undefined, { month: 'short', year: 'numeric' })
  );

  constructor() {
    effect(() => {
      const u = this.currentUser();
      if (!u) return;
      this.goalCalories.set(u.dailyActiveCaloriesTarget);
      this.goalMinutes.set(u.dailyWorkoutMinutesTarget);
      this.goalWeight.set(u.monthlyWeightGoalLb);
      this.morningStart.set(toHHmm(u.morningWindowStart));
      this.morningEnd.set(toHHmm(u.morningWindowEnd));
      this.kitchenStart.set(toHHmm(u.kitchenClosedStart));
      this.kitchenEnd.set(toHHmm(u.kitchenClosedEnd));
      this.morningReminder.set(u.morningReminderEnabled);
      this.kitchenNudge.set(u.kitchenNudgeEnabled);
      this.leaderboard.set(u.leaderboardOptIn);
    });
  }

  ngOnInit(): void {
    this.meApi.getMe().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => undefined
    });
  }

  protected onDeleted(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }

  protected onSignOut(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }
}

function toHHmm(value: string | null | undefined): string {
  if (!value) return '';
  const parts = value.split(':');
  if (parts.length < 2) return value;
  return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`;
}
