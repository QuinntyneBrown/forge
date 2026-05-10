// Acceptance Test for Bug 028
// Description: /sign-up should expose a Confirm password field that must
// match the Password field; submit stays disabled while they differ.

import { expect, test } from '@playwright/test';

test.describe('Sign-up confirm password', () => {
  test('renders confirm field and gates submit on password match', async ({ page }) => {
    await page.goto('/sign-up');
    await page.waitForLoadState('networkidle');

    const confirm = page.getByTestId('sign-up-confirm-password');
    await expect(confirm).toBeVisible();

    // Fill the form with mismatched passwords; submit should remain disabled.
    await page.getByTestId('sign-up-first-name').fill('A');
    await page.getByTestId('sign-up-last-name').fill('B');
    await page.getByTestId('sign-up-email').fill('confirm@example.com');
    await page.getByTestId('sign-up-password').fill('AaaBbb!12345');
    await confirm.fill('Different!12345');

    const tos = page.getByTestId('sign-up-tos').locator('input[type=checkbox]');
    await tos.check();

    const submit = page.getByTestId('sign-up-submit');
    await expect(submit).toBeDisabled();

    // Now match — submit becomes enabled.
    await confirm.fill('AaaBbb!12345');
    await expect(submit).toBeEnabled();
  });
});
