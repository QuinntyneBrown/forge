// Acceptance Test for Bug 003
// Description: The sign-in page password field should expose a "show password"
// toggle (eye icon) that switches the input's `type` between `password` and
// `text`. Mock: docs/mocks/sign-in.html line 85.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

test.describe('Sign in: show password toggle', () => {
  test('toggles the password input between masked and visible', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();

    const password = page.getByTestId('sign-in-password');
    await password.fill('hunter2');
    await expect(password).toHaveAttribute('type', 'password');

    const toggle = page.getByTestId('sign-in-show-password');
    await expect(toggle).toBeVisible();

    await toggle.click();
    await expect(password).toHaveAttribute('type', 'text');

    await toggle.click();
    await expect(password).toHaveAttribute('type', 'password');
  });
});
