// Acceptance Test
// Traces to: Bug 028 (sign-up missing confirm password field)
// Description: The mock at docs/mocks/sign-up.html renders five inputs
// (first name, last name, email, password, confirm password). The implementation
// is missing the confirm password field. This spec asserts the field exists and
// that a cross-field validator blocks submission when the values do not match.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignUpPage } from '../pages/sign-up.page';

test.describe('Sign-up confirm password field (Bug 028)', () => {
  test('renders a Confirm password field matching the mock label', async ({ page }) => {
    const signUp = new SignUpPage(page);
    await signUp.goto();

    await expect(signUp.confirmPasswordInput).toBeVisible();
    await expect(signUp.confirmPasswordInput).toHaveAttribute('type', 'password');
    // The mock uses the label "Confirm password" — the rendered Material form
    // field exposes that label adjacent to the input.
    await expect(page.getByText('Confirm password', { exact: true })).toBeVisible();
  });

  test('blocks submission when password and confirm password do not match', async ({ page }) => {
    const signUp = new SignUpPage(page);
    const email = `signup-mismatch-${Date.now()}@forgefit.app`;

    await signUp.goto();
    await signUp.firstNameInput.fill('Sign');
    await signUp.lastNameInput.fill('Up');
    await signUp.emailInput.fill(email);
    await signUp.passwordInput.fill('PasswordOne!23');
    await signUp.confirmPasswordInput.fill('PasswordTwo!23');
    await signUp.confirmPasswordInput.blur();
    await signUp.tosCheckbox.check();

    // The form is invalid while the two passwords diverge, so the submit
    // button must be disabled and the field-level mismatch error must render.
    await expect(signUp.submitButton).toBeDisabled();
    await expect(signUp.confirmPasswordError).toBeVisible();
    await expect(signUp.confirmPasswordError).toContainText(/do not match/i);
    // Sanity-check we never navigated away from /sign-up.
    await expect(page).toHaveURL(/\/sign-up$/);
  });

  test('allows submission when password and confirm password match', async ({ page }) => {
    const signUp = new SignUpPage(page);
    const dashboard = new DashboardPage(page);
    const email = `signup-match-${Date.now()}@forgefit.app`;
    const password = 'Password123!';

    await signUp.goto();
    await signUp.firstNameInput.fill('Sign');
    await signUp.lastNameInput.fill('Up');
    await signUp.emailInput.fill(email);
    await signUp.passwordInput.fill(password);
    await signUp.confirmPasswordInput.fill(password);
    await signUp.tosCheckbox.check();
    await signUp.submitButton.click();

    await dashboard.waitForLoad();
    await expect(page).not.toHaveURL(/\/sign-up$/);
  });
});
