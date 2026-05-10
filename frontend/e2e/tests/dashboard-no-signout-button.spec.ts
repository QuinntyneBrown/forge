// Acceptance Test for Bug 027
// Description: The /dashboard route must NOT render a "Sign out" button in
// its topbar/greeting block. The mock at docs/mocks/dashboard.html does not
// contain such a button — the only sign-out affordance lives on the Profile
// page (per Bug 011's resolution). This spec asserts the dashboard topbar is
// free of any Sign-out control while confirming Profile remains reachable
// via the existing primary nav.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard topbar — no sign-out button (Bug 027)', () => {
  test('dashboard does not expose a sign-out button and Profile is reachable via nav', async ({ page, request }) => {
    const email = `nosignout-${Date.now()}@forgefit.app`;
    const register = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'NoSign', lastName: 'Out', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(register.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    // Dashboard topbar must not contain the sign-out testid.
    await expect(page.getByTestId('sign-out')).toHaveCount(0);

    // Nor any button/link with the accessible name "Sign out".
    await expect(
      page.getByRole('button', { name: /^sign out$/i })
    ).toHaveCount(0);
    await expect(
      page.getByRole('link', { name: /^sign out$/i })
    ).toHaveCount(0);

    // Sanity: Profile remains reachable via the primary nav so users can
    // still sign out via the Profile page (Bug 011).
    const profileNav = page
      .getByRole('link', { name: /profile/i })
      .or(page.getByRole('button', { name: /profile/i }))
      .first();
    await expect(profileNav).toBeVisible();
    await profileNav.click();
    await page.waitForURL('**/profile');
    await expect(page.getByTestId('profile-sign-out-button')).toBeVisible();
  });
});
