// Acceptance Test for Bug 020 (auth mocks)
// Description: Sign-up should expose a perks list inside the hero, a password
// strength meter under the password field, and a Terms-of-Service checkbox
// above the submit. Password-reset should expose a confirmation/sent state.

import { expect, test } from '@playwright/test';

test.describe('Auth mock extras', () => {
  test('sign-up renders perks list, password strength meter, and ToS checkbox', async ({ page }) => {
    await page.goto('/sign-up');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('sign-up-hero-perks')).toBeVisible();
    const perks = page.locator('[data-testid="sign-up-hero-perk"]');
    expect(await perks.count()).toBeGreaterThanOrEqual(3);

    // Strength meter appears once the user types into the password field.
    await page.getByTestId('sign-up-password').fill('Password1!');
    await expect(page.getByTestId('sign-up-password-strength')).toBeVisible();

    await expect(page.getByTestId('sign-up-tos')).toBeVisible();
  });

  test('password-reset renders a sent confirmation card after submit', async ({ page }) => {
    await page.goto('/password-reset');
    await page.waitForLoadState('networkidle');

    await page.getByTestId('password-reset-request-email').fill('test@forgefit.app');
    await page.getByTestId('password-reset-request-submit').click();

    await expect(page.getByTestId('password-reset-sent-card')).toBeVisible();
    await expect(page.getByTestId('password-reset-resend')).toBeVisible();
  });
});
