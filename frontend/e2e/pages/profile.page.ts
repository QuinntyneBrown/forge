import { Locator, Page } from '@playwright/test';

export class ProfilePage {
  readonly firstName: Locator;
  readonly lastName: Locator;
  readonly email: Locator;
  readonly units: Locator;
  readonly timeZone: Locator;
  readonly caloriesTarget: Locator;
  readonly minutesTarget: Locator;
  readonly saveButton: Locator;
  readonly savedBanner: Locator;

  // Bug 011 — hero + cards
  readonly hero: Locator;
  readonly heroAvatar: Locator;
  readonly heroDisplayName: Locator;
  readonly heroTierChip: Locator;
  readonly goalsCard: Locator;
  readonly goalCalories: Locator;
  readonly goalMinutes: Locator;
  readonly goalWeight: Locator;
  readonly windowsCard: Locator;
  readonly morningStart: Locator;
  readonly morningEnd: Locator;
  readonly kitchenStart: Locator;
  readonly kitchenEnd: Locator;
  readonly integrationsCard: Locator;
  readonly morningReminderToggle: Locator;
  readonly kitchenNudgeToggle: Locator;
  readonly leaderboardToggle: Locator;
  readonly saveCard: Locator;
  readonly saveAllButton: Locator;
  readonly signOutButton: Locator;

  constructor(private readonly page: Page) {
    this.firstName = page.getByTestId('profile-first-name');
    this.lastName = page.getByTestId('profile-last-name');
    this.email = page.getByTestId('profile-email');
    this.units = page.getByTestId('profile-units');
    this.timeZone = page.getByTestId('profile-time-zone');
    this.caloriesTarget = page.getByTestId('profile-calories-target');
    this.minutesTarget = page.getByTestId('profile-minutes-target');
    this.saveButton = page.getByTestId('profile-save');
    this.savedBanner = page.getByTestId('profile-saved');

    this.hero = page.getByTestId('profile-hero');
    this.heroAvatar = page.getByTestId('profile-hero-avatar');
    this.heroDisplayName = page.getByTestId('profile-hero-name');
    this.heroTierChip = page.getByTestId('profile-hero-tier');

    this.goalsCard = page.getByTestId('profile-goals-card');
    this.goalCalories = page.getByTestId('profile-goal-calories');
    this.goalMinutes = page.getByTestId('profile-goal-minutes');
    this.goalWeight = page.getByTestId('profile-goal-weight');

    this.windowsCard = page.getByTestId('profile-windows-card');
    this.morningStart = page.getByTestId('profile-morning-window-start');
    this.morningEnd = page.getByTestId('profile-morning-window-end');
    this.kitchenStart = page.getByTestId('profile-kitchen-closed-start');
    this.kitchenEnd = page.getByTestId('profile-kitchen-closed-end');

    this.integrationsCard = page.getByTestId('profile-integrations-card');
    this.morningReminderToggle = page.getByTestId('profile-morning-reminder-toggle');
    this.kitchenNudgeToggle = page.getByTestId('profile-kitchen-nudge-toggle');
    this.leaderboardToggle = page.getByTestId('profile-leaderboard-toggle');

    this.saveCard = page.getByTestId('profile-save-card');
    this.saveAllButton = page.getByTestId('profile-save-button');
    this.signOutButton = page.getByTestId('profile-sign-out-button');
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
  }
}
