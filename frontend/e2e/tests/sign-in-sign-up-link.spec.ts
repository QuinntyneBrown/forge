// Acceptance Test for Bug 002
// Description: The sign-in page should display a "Create an account" link that
// navigates to /sign-up. The mock at docs/mocks/sign-in.html shows the link
// in an auth footer reading "New to Forge Fit? Create an account".

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

test.describe('Sign in: sign-up link', () => {
  test('renders a Create an account link that navigates to /sign-up', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();

    const link = page.getByTestId('sign-in-sign-up-link');
    await expect(link).toBeVisible();
    await expect(link).toHaveText(/create an account/i);

    await link.click();
    await page.waitForURL(/\/sign-up$/);
  });
});
