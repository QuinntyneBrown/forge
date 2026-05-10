import { Locator, Page } from '@playwright/test';

export class SignUpPage {
  readonly firstNameInput: Locator;
  readonly lastNameInput: Locator;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly confirmPasswordError: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly hero: Locator;
  readonly formCard: Locator;
  readonly pageRoot: Locator;
  readonly perksList: Locator;
  readonly perks: Locator;
  readonly passwordStrengthMeter: Locator;
  readonly tosCheckbox: Locator;

  constructor(private readonly page: Page) {
    this.firstNameInput = page.getByTestId('sign-up-first-name');
    this.lastNameInput = page.getByTestId('sign-up-last-name');
    this.emailInput = page.getByTestId('sign-up-email');
    this.passwordInput = page.getByTestId('sign-up-password');
    this.confirmPasswordInput = page.getByTestId('sign-up-confirm-password');
    this.confirmPasswordError = page.getByTestId('sign-up-confirm-password-error');
    this.submitButton = page.getByTestId('sign-up-submit');
    this.errorMessage = page.getByTestId('sign-up-error');
    this.hero = page.locator('.sign-up-page__hero');
    this.formCard = page.locator('.sign-up-page__card');
    this.pageRoot = page.locator('.sign-up-page');
    this.perksList = page.getByTestId('sign-up-hero-perks');
    this.perks = page.getByTestId('sign-up-hero-perk');
    this.passwordStrengthMeter = page.getByTestId('sign-up-password-strength');
    // The wrapper label hosts the checkbox input.
    this.tosCheckbox = page.getByTestId('sign-up-tos').locator('input[type="checkbox"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/sign-up');
  }

  async signUp(firstName: string, lastName: string, email: string, password: string): Promise<void> {
    await this.firstNameInput.fill(firstName);
    await this.lastNameInput.fill(lastName);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    // Bug 028 added a Confirm password field with a cross-field validator; the
    // helper fills it with the same value so existing specs still submit.
    await this.confirmPasswordInput.fill(password);
    // Bug 020 introduced a required Terms-of-Service checkbox; the submit button
    // is now disabled until the checkbox is ticked.
    await this.tosCheckbox.check();
    await this.submitButton.click();
  }
}
