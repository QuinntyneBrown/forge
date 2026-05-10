import { Locator, Page } from '@playwright/test';

export class DashboardPage {
  readonly greeting: Locator;
  readonly dailyRingCard: Locator;
  readonly dailyRingValue: Locator;
  readonly dailyRingMinutes: Locator;
  readonly streakCard: Locator;
  readonly streakDays: Locator;
  readonly streakMultiplier: Locator;
  readonly weightProgressCard: Locator;
  readonly weightProgressGoal: Locator;
  readonly weightProgressMtd: Locator;
  readonly tierCard: Locator;
  readonly tierName: Locator;
  readonly tierBalance: Locator;
  readonly leaderboardCard: Locator;
  readonly leaderboardEmpty: Locator;
  readonly leaderboardList: Locator;

  // Bug 018: hero gradient + new content cards
  readonly hero: Locator;
  readonly heroEyebrow: Locator;
  readonly heroStartWorkoutCta: Locator;
  readonly heroViewSessionsCta: Locator;
  readonly eatingWindowCard: Locator;
  readonly eatingWindowTitle: Locator;
  readonly todaysSessionsCard: Locator;
  readonly todaysSessionsList: Locator;
  readonly todaysSessionsItems: Locator;
  readonly badgeRow: Locator;
  readonly badgeItems: Locator;
  readonly sparkline: Locator;

  constructor(private readonly page: Page) {
    this.greeting = page.getByTestId('dashboard-greeting');

    this.dailyRingCard = page.getByTestId('daily-ring-card');
    this.dailyRingValue = page.getByTestId('daily-ring-card-value');
    this.dailyRingMinutes = page.getByTestId('daily-ring-card-minutes');

    this.streakCard = page.getByTestId('streak-card');
    this.streakDays = page.getByTestId('streak-card-days');
    this.streakMultiplier = page.getByTestId('streak-card-multiplier');

    this.weightProgressCard = page.getByTestId('weight-progress-card');
    this.weightProgressGoal = page.getByTestId('weight-progress-card-goal');
    this.weightProgressMtd = page.getByTestId('weight-progress-card-mtd');

    this.tierCard = page.getByTestId('tier-card');
    this.tierName = page.getByTestId('tier-card-name');
    this.tierBalance = page.getByTestId('tier-card-balance');

    this.leaderboardCard = page.getByTestId('leaderboard-card');
    this.leaderboardEmpty = page.getByTestId('leaderboard-card-empty');
    this.leaderboardList = page.getByTestId('leaderboard-card-list');

    this.hero = page.getByTestId('dashboard-hero');
    this.heroEyebrow = page.getByTestId('dashboard-hero-eyebrow');
    this.heroStartWorkoutCta = page.getByTestId('dashboard-hero-start-workout');
    this.heroViewSessionsCta = page.getByTestId('dashboard-hero-view-sessions');

    this.eatingWindowCard = page.getByTestId('eating-window-card');
    this.eatingWindowTitle = page.getByTestId('eating-window-card-title');

    this.todaysSessionsCard = page.getByTestId('todays-sessions-card');
    this.todaysSessionsList = page.getByTestId('todays-sessions-card-list');
    this.todaysSessionsItems = page.getByTestId('todays-sessions-card-item');

    this.badgeRow = page.getByTestId('dashboard-badge-row');
    this.badgeItems = page.getByTestId('dashboard-badge');
    this.sparkline = page.getByTestId('dashboard-sparkline');
  }

  async waitForLoad(): Promise<void> {
    await this.page.waitForURL('**/dashboard');
    await this.greeting.waitFor();
  }

  secondaryCta(label: string | RegExp): Locator {
    return this.page.getByRole('button', { name: label }).or(
      this.page.getByRole('link', { name: label })
    );
  }
}
