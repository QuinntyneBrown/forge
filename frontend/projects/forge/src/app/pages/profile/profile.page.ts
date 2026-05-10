import { Component, Inject, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import {
  AUTH_SERVICE,
  CurrentUser,
  IAuthService,
  IMeService,
  IProfileService,
  IRewardsService,
  ME_SERVICE,
  PROFILE_SERVICE,
  REWARDS_SERVICE,
  Tier
} from 'api';
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
  imports: [AppShellComponent, ProfileFormComponent, FormsModule],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss'
})
export class ProfilePage implements OnInit {
  private readonly auth = inject(AuthStateService);
  private readonly router = inject(Router);
  private readonly meApi = inject<IMeService>(ME_SERVICE);
  private readonly profileApi = inject<IProfileService>(PROFILE_SERVICE);
  private readonly rewardsApi = inject<IRewardsService>(REWARDS_SERVICE);
  private readonly authApi = inject<IAuthService>(AUTH_SERVICE);

  protected readonly destinations = PRIMARY_DESTINATIONS;
  protected readonly currentUser = signal<CurrentUser | null>(null);
  protected readonly tier = signal<Tier | null>(null);
  protected readonly saving = signal(false);
  protected readonly saved = signal(false);
  protected readonly saveError = signal<string | null>(null);

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
  protected readonly appleWatchSync = signal(true);

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

  // Tier label sourced from GET /api/tier; falls back to "Bronze" so the
  // mock-style chip is never empty before the request resolves.
  protected readonly tierLabel = computed(() => {
    const t = this.tier();
    return t?.name ? `Tier · ${t.name}` : 'Tier · Bronze';
  });

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
    this.rewardsApi.getCurrentTier().subscribe({
      next: (t) => this.tier.set(t),
      error: () => undefined
    });
  }

  protected toggleMorningReminder(): void {
    this.morningReminder.update((v) => !v);
  }

  protected toggleKitchenNudge(): void {
    this.kitchenNudge.update((v) => !v);
  }

  protected toggleLeaderboard(): void {
    this.leaderboard.update((v) => !v);
  }

  protected toggleAppleWatch(): void {
    this.appleWatchSync.update((v) => !v);
  }

  protected onChangeTheme(): void {
    // Placeholder until the theme picker dialog ships.
  }

  // Account deletion lives in the Save card now (was previously inside the
  // ProfileFormComponent). Same testids preserved for account-deletion e2e.
  protected readonly confirmingDelete = signal(false);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected requestDelete(): void {
    if (this.deleting()) return;
    this.deleteError.set(null);
    this.confirmingDelete.set(true);
  }

  protected cancelDelete(): void {
    if (this.deleting()) return;
    this.confirmingDelete.set(false);
  }

  protected confirmDelete(): void {
    if (this.deleting()) return;
    this.deleting.set(true);
    this.deleteError.set(null);
    this.meApi.deleteMe().subscribe({
      next: () => {
        this.deleting.set(false);
        this.confirmingDelete.set(false);
        this.onDeleted();
      },
      error: (err) => {
        this.deleting.set(false);
        this.deleteError.set(err?.error?.title ?? 'Could not delete account.');
      }
    });
  }

  protected onSaveAll(): void {
    if (this.saving()) {
      return;
    }
    const u = this.currentUser();
    if (!u) {
      return;
    }
    const cal = this.goalCalories() ?? u.dailyActiveCaloriesTarget;
    const min = this.goalMinutes() ?? u.dailyWorkoutMinutesTarget;
    const weight = this.goalWeight() ?? u.monthlyWeightGoalLb;

    this.saving.set(true);
    this.saveError.set(null);
    this.saved.set(false);

    forkJoin([
      this.profileApi.updateProfile({
        email: u.email,
        firstName: u.firstName,
        lastName: u.lastName,
        units: u.units,
        timeZoneId: u.timeZoneId,
        dailyActiveCaloriesTarget: cal,
        dailyWorkoutMinutesTarget: min
      }),
      this.profileApi.setWeightGoal(weight),
      this.profileApi.updateMorningWindow({
        start: this.morningStart(),
        end: this.morningEnd(),
        reminderEnabled: this.morningReminder()
      }),
      this.profileApi.updateKitchenWindow({
        start: this.kitchenStart(),
        end: this.kitchenEnd(),
        nudgeEnabled: this.kitchenNudge()
      }),
      this.profileApi.setLeaderboardOptIn(this.leaderboard())
    ]).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err) => {
        this.saving.set(false);
        this.saveError.set(err?.error?.title ?? 'Could not save changes.');
      }
    });
  }

  protected onDeleted(): void {
    this.auth.clear();
    this.router.navigate(['/sign-in']);
  }

  protected onSignOut(): void {
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

function toHHmm(value: string | null | undefined): string {
  if (!value) return '';
  const parts = value.split(':');
  if (parts.length < 2) return value;
  return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`;
}
