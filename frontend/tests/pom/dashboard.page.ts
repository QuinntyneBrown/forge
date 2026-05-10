import { Locator, Page } from '@playwright/test';

export class DashboardPage {
  readonly greeting: Locator;
  readonly signOutButton: Locator;
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

  constructor(private readonly page: Page) {
    this.greeting = page.getByTestId('dashboard-greeting');
    this.signOutButton = page.getByTestId('sign-out');

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
  }

  async waitForLoad(): Promise<void> {
    await this.page.waitForURL('**/dashboard');
    await this.greeting.waitFor();
  }
}
