// Acceptance Test for Bug 006
// Description: The /rewards page should follow docs/mocks/rewards.html
// structure: an orange "Points balance" hero, a "Recent achievements" section,
// an "In-flight" progress section, and a "Redeem" rewards list.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Rewards mock alignment', () => {
  test('renders balance hero, achievements, in-flight, and redeem sections', async ({
    page,
    request
  }) => {
    const email = `rewards-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'R', lastName: 'M', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    await page.getByTestId('sign-in-remember-me').click();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    await page.goto('/rewards');
    await page.waitForLoadState('networkidle');

    const balance = page.getByTestId('rewards-balance-hero');
    await expect(balance).toBeVisible();
    await expect(balance).toContainText(/points balance/i);
    await expect(page.getByTestId('rewards-balance-amount')).toBeVisible();

    const achievements = page.getByTestId('rewards-achievements-section');
    await expect(achievements).toBeVisible();
    await expect(achievements).toContainText(/recent achievements/i);

    const inFlight = page.getByTestId('rewards-in-flight-section');
    await expect(inFlight).toBeVisible();
    await expect(inFlight).toContainText(/in-flight/i);

    const redeem = page.getByTestId('rewards-redeem-section');
    await expect(redeem).toBeVisible();
    await expect(redeem).toContainText(/redeem/i);
  });
});
