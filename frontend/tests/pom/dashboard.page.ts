import { Locator, Page } from '@playwright/test';

export class DashboardPage {
  readonly greeting: Locator;
  readonly healthBadge: Locator;
  readonly signOutButton: Locator;

  constructor(private readonly page: Page) {
    this.greeting = page.getByTestId('dashboard-greeting');
    this.healthBadge = page.getByTestId('health-badge');
    this.signOutButton = page.getByTestId('sign-out');
  }

  async waitForLoad(): Promise<void> {
    await this.page.waitForURL('**/dashboard');
    await this.greeting.waitFor();
  }
}
