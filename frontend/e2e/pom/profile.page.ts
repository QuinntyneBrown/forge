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
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
  }
}
