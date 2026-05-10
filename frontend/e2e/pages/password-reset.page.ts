import { Locator, Page } from '@playwright/test';

export class PasswordResetPage {
  readonly emailInput: Locator;
  readonly submitButton: Locator;
  readonly hero: Locator;
  readonly formCard: Locator;
  readonly pageRoot: Locator;
  readonly successCard: Locator;
  readonly successHeading: Locator;
  readonly resendButton: Locator;

  constructor(private readonly page: Page) {
    this.emailInput = page.getByTestId('password-reset-request-email');
    this.submitButton = page.getByTestId('password-reset-request-submit');
    this.hero = page.locator('.password-reset-page__hero');
    this.formCard = page.locator('.password-reset-page__card');
    this.pageRoot = page.locator('.password-reset-page');
    this.successCard = page.getByTestId('password-reset-sent-card');
    this.successHeading = page.getByTestId('password-reset-request-confirmation');
    this.resendButton = page.getByTestId('password-reset-resend');
  }

  async goto(): Promise<void> {
    await this.page.goto('/password-reset');
  }

  async requestReset(email: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.submitButton.click();
  }
}
