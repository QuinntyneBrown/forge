// Acceptance Test for Bug 025
// Description: Profile page should have a single Save button (in the Save
// card), no orphan Danger zone copy in the Account card, and the Integrations
// card should include Apple Watch sync and Theme rows.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Profile cleanup', () => {
  test('single save button, no orphan danger zone, watch + theme rows', async ({ page, request }) => {
    const email = `pclean-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'P', lastName: 'C', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/profile');
    await page.waitForLoadState('networkidle');

    // Single Save changes button (the one in the Save card)
    const saveButtons = page.getByRole('button', { name: /save changes/i });
    await expect(saveButtons).toHaveCount(1);

    // No "Danger zone" copy in the Account card.
    const accountCard = page.getByTestId('profile-account-card');
    await expect(accountCard).not.toContainText(/danger zone/i);

    // Apple Watch sync row in Integrations.
    const integrations = page.getByTestId('profile-integrations-card');
    await expect(integrations.getByTestId('profile-apple-watch-row')).toBeVisible();

    // Theme row with a Change link instead of a switch.
    await expect(integrations.getByTestId('profile-theme-row')).toBeVisible();
    await expect(integrations.getByTestId('profile-theme-change-link')).toBeVisible();
  });
});
