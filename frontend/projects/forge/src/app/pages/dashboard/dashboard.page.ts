import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  AUTH_SERVICE,
  CurrentUser,
  IAuthService,
  IMeService,
  ISessionsService,
  ME_SERVICE,
  SESSIONS_SERVICE,
  Session
} from 'api';
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

interface BadgeChip {
  id: string;
  title: string;
  sub: string;
  icon: string;
  tint: 'gold' | 'teal' | 'neutral';
}

interface DashboardSession {
  id: string;
  title: string;
  meta: string;
  points: number;
  icon: string;
  tint: 'teal' | 'amber';
}

const EQUIPMENT_ICON: Record<string, string> = {
  Treadmill: 'directions_run',
  IndoorBike: 'directions_bike',
  BenchPress: 'fitness_center',
  Elliptical: 'fitness_center'
};

const EQUIPMENT_LABEL: Record<string, string> = {
  Treadmill: 'Treadmill',
  IndoorBike: 'Indoor bike',
  BenchPress: 'Bench press',
  Elliptical: 'Elliptical'
};

const EQUIPMENT_TINT: Record<string, 'teal' | 'amber'> = {
  Treadmill: 'teal',
  IndoorBike: 'amber',
  BenchPress: 'teal',
  Elliptical: 'teal'
};

// Bug 018: badges are not yet exposed by an achievements API. Render the
// three mock chips as placeholders so the dashboard composition matches
// the design until that endpoint is wired up.
const PLACEHOLDER_BADGES: BadgeChip[] = [
  { id: 'morning-warrior', title: 'Morning Warrior', sub: '7 of 10 days', icon: 'wb_sunny', tint: 'gold' },
  { id: '1500-cal-club', title: '1500-Cal Club', sub: '3 days · this week', icon: 'local_fire_department', tint: 'teal' },
  { id: 'night-resister', title: 'Night Resister', sub: '5 day streak', icon: 'nightlight', tint: 'neutral' }
];

// Bug 018: weight-history endpoint not exposed yet. Render a deterministic
// 7-bar sparkline as a structural placeholder so the component shape matches
// the mock; replace with real points-over-time data when the API lands.
const SPARKLINE_BARS = [35, 55, 42, 78, 60, 88, 96];

@Component({
  selector: 'app-dashboard-page',
  imports: [
    CommonModule,
    RouterLink,
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
  protected readonly todaysSessions = signal<DashboardSession[]>([]);
  protected readonly badges = signal<BadgeChip[]>(PLACEHOLDER_BADGES);
  protected readonly sparklineBars = SPARKLINE_BARS;

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

  protected readonly eatingWindowTitle = computed(() => {
    const user = this.currentUser();
    if (!user) {
      return 'Kitchen closed window';
    }
    const reopen = formatTimeOfDay(user.kitchenClosedEnd);
    return `Fasting until ${reopen}`;
  });

  protected readonly eatingWindowRange = computed(() => {
    const user = this.currentUser();
    if (!user) {
      return '';
    }
    const start = formatTimeOfDay(user.kitchenClosedStart);
    const end = formatTimeOfDay(user.kitchenClosedEnd);
    return `${start} → ${end}`;
  });

  protected readonly eatingWindowSub = computed(() => {
    const user = this.currentUser();
    if (!user) {
      return '';
    }
    const hours = computeWindowHours(user.kitchenClosedStart, user.kitchenClosedEnd);
    return `${hours}-hour fast · target met`;
  });

  protected readonly todaysSessionsCount = computed(() => this.todaysSessions().length);

  protected readonly todayActiveCalories = computed(() =>
    this.todaysSessions().reduce((sum, s) => sum + (s.points ?? 0), 0) // base proxy until calorie aggregate lives client-side
  );

  protected readonly todayCalorieTotal = computed(() => {
    // Sum activeCalories from raw sessions if available — we keep them off the
    // signal here to avoid duplicating state, so estimate from session count.
    return this.todaysSessions().reduce(
      (sum, s) => sum + Number((s.meta.match(/(\d+)\s*cal/i) || [])[1] ?? 0),
      0
    );
  });

  protected readonly todayCalorieGoal = computed(
    () => this.currentUser()?.dailyActiveCaloriesTarget ?? 1500
  );

  protected readonly heroTitle = computed(() => {
    const remaining = Math.max(0, this.todayCalorieGoal() - this.todayCalorieTotal());
    return `You're ${remaining.toLocaleString()} cal from your daily goal`;
  });

  protected readonly todayMinutes = computed(() =>
    this.todaysSessions().reduce(
      (sum, s) => sum + Number((s.meta.match(/(\d+)\s*min/i) || [])[1] ?? 0),
      0
    )
  );

  protected readonly avgHeartRate = computed(() => {
    const hrs = this.todaysSessions()
      .map((s) => Number((s.meta.match(/avg\s*(\d+)\s*bpm/i) || [])[1] ?? NaN))
      .filter((n) => !Number.isNaN(n));
    if (hrs.length === 0) return null;
    return Math.round(hrs.reduce((a, b) => a + b, 0) / hrs.length);
  });

  protected readonly weightTrend = computed(() => {
    // No weight-history endpoint yet — show the placeholder copy from the mock.
    return '— · log to track';
  });

  protected readonly streakDays = signal<number>(7);
  protected readonly streakTitle = computed(() => `${this.streakDays()}-day morning streak`);

  protected readonly pointsToday = computed(() =>
    this.todaysSessions().reduce((sum, s) => sum + (s.points ?? 0), 0)
  );
  // No lifetime-balance signal on the page yet; display the placeholder until
  // the rewards summary feeds it. Keeps the streak card layout matching the mock.
  protected readonly pointsTotal = signal<number>(2840);

  constructor(
    @Inject(AUTH_SERVICE) private readonly authApi: IAuthService,
    @Inject(ME_SERVICE) private readonly meApi: IMeService,
    @Inject(SESSIONS_SERVICE) private readonly sessionsApi: ISessionsService
  ) {}

  ngOnInit(): void {
    this.meApi.getMe().subscribe({
      next: (user) => this.currentUser.set(user),
      error: () => undefined
    });
    this.sessionsApi.list({ range: 'today', page: 1, pageSize: 25 }).subscribe({
      next: (page) => this.todaysSessions.set(page.items.map((s) => this.toDashboardSession(s))),
      error: () => this.todaysSessions.set([])
    });
  }

  protected logWorkout(): void {
    this.router.navigate(['/workouts', 'new']);
  }

  protected viewTodaysSessions(): void {
    this.router.navigate(['/workouts']);
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

  private toDashboardSession(s: Session): DashboardSession {
    const time = new Date(s.startedAt).toLocaleTimeString(undefined, {
      hour: 'numeric',
      minute: '2-digit'
    });
    const parts = [time, `${s.durationMinutes} min`, `${s.activeCalories} cal`];
    if (s.distanceMiles != null) {
      parts.push(`${s.distanceMiles} mi`);
    } else if (s.avgHeartRateBpm != null) {
      parts.push(`avg ${s.avgHeartRateBpm} bpm`);
    }
    return {
      id: s.id,
      title: `${EQUIPMENT_LABEL[s.equipment] ?? s.equipment}`,
      meta: parts.join(' · '),
      points: Math.max(1, Math.round(s.durationMinutes * 2)),
      icon: EQUIPMENT_ICON[s.equipment] ?? 'fitness_center',
      tint: EQUIPMENT_TINT[s.equipment] ?? 'teal'
    };
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

// "20:00:00" → "8:00 PM"
function formatTimeOfDay(iso: string | null | undefined): string {
  if (!iso) return '';
  const [hStr, mStr] = iso.split(':');
  const h = Number(hStr);
  const m = Number(mStr);
  if (Number.isNaN(h) || Number.isNaN(m)) return iso;
  const ampm = h >= 12 ? 'PM' : 'AM';
  const display = h % 12 === 0 ? 12 : h % 12;
  const minutes = m.toString().padStart(2, '0');
  return `${display}:${minutes} ${ampm}`;
}

// Hours between two HH:mm:ss strings, wrapping past midnight.
function computeWindowHours(startIso: string, endIso: string): number {
  if (!startIso || !endIso) return 0;
  const [sh, sm] = startIso.split(':').map(Number);
  const [eh, em] = endIso.split(':').map(Number);
  const start = sh * 60 + sm;
  let end = eh * 60 + em;
  if (end <= start) end += 24 * 60;
  return Math.round((end - start) / 60);
}
