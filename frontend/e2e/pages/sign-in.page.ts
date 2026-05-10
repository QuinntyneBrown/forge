import { Locator, Page } from '@playwright/test';

export class SignInPage {
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly hero: Locator;
  readonly formCard: Locator;
  readonly pageRoot: Locator;

  constructor(private readonly page: Page) {
    this.emailInput = page.getByTestId('sign-in-email');
    this.passwordInput = page.getByTestId('sign-in-password');
    this.submitButton = page.getByTestId('sign-in-submit');
    this.errorMessage = page.getByTestId('sign-in-error');
    this.hero = page.locator('.sign-in-page__hero');
    this.formCard = page.locator('.sign-in-page__card');
    this.pageRoot = page.locator('.sign-in-page');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-in');
  }

  async signIn(email: string, password: string): Promise<void> {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }
}
