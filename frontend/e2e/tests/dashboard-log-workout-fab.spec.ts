// Acceptance Test for Bug 031
// Description: Dashboard should expose a fixed orange "+ Log workout" FAB
// pinned to the viewport bottom-right (not an inline pill in the Today's
// sessions card header).

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard log-workout FAB', () => {
  test('renders a fixed-position FAB and no inline pill inside Today\'s sessions', async ({
    page,
    request
  }) => {
    const email = `dfab-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'D', lastName: 'F', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    await page.setViewportSize({ width: 390, height: 740 });
    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('networkidle');

    const fab = page.getByTestId('dashboard-log-workout-fab');
    await expect(fab).toBeVisible();
    const position = await fab.evaluate((el) => getComputedStyle(el).position);
    expect(position).toBe('fixed');

    // No inline "Log workout" pill inside the Today's sessions card.
    const todaysSessions = page.getByTestId('todays-sessions-card');
    await expect(todaysSessions).toBeVisible();
    await expect(
      todaysSessions.getByRole('button', { name: /log workout/i })
    ).toHaveCount(0);
  });
});
