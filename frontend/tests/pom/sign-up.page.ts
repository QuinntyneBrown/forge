import { Locator, Page } from '@playwright/test';

export class SignUpPage {
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;

  constructor(private readonly page: Page) {
    this.firstNameInput = page.getByTestId('sign-up-first-name');
    this.lastNameInput = page.getByTestId('sign-up-last-name');
    this.emailInput = page.getByTestId('sign-up-email');
    this.passwordInput = page.getByTestId('sign-up-password');
    this.submitButton = page.getByTestId('sign-up-submit');
    this.errorMessage = page.getByTestId('sign-up-error');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-up');
  }

  async signUp(firstName: string, lastName: string, email: string, password: string): Promise<void> {
    await this.firstNameInput.fill(firstName);
    await this.lastNameInput.fill(lastName);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }
}
