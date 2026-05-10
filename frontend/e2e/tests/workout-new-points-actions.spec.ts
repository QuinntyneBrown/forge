// Acceptance Test for Bug 008
// Description: /workouts/new should add Points Breakdown and Actions sections
// below the Session details card, and the page surface should be white per
// docs/mocks/workout-detail.html.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Workout new — points + actions sections', () => {
  test('renders Points Breakdown and Actions cards on a white surface', async ({
    page,
    request
  }) => {
    const email = `wn8-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'WN8', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    await page.getByTestId('sign-in-remember-me').click();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    await page.goto('/workouts/new');
    await page.waitForLoadState('networkidle');

    const points = page.getByTestId('workout-new-points-card');
    await expect(points).toBeVisible();
    await expect(points).toContainText(/points breakdown/i);
    // The card should expose at least a base row and a total row.
    await expect(page.getByTestId('workout-new-points-base')).toBeVisible();
    await expect(page.getByTestId('workout-new-points-total')).toBeVisible();

    const actions = page.getByTestId('workout-new-actions-card');
    await expect(actions).toBeVisible();
    await expect(actions).toContainText(/actions/i);
    // Save (form-level) and Cancel both live within the actions card.
    await expect(actions.getByTestId('workout-detail-form-submit')).toBeVisible();
    await expect(actions.getByTestId('workout-new-cancel')).toBeVisible();

    // The body wrapper around the cards should be on a white background.
    const bodyBg = await page
      .getByTestId('workout-new-body')
      .evaluate((el) => getComputedStyle(el).backgroundColor);
    // Accept any near-white CSS color (white or transparent on a white root).
    expect(['rgb(255, 255, 255)', 'rgba(0, 0, 0, 0)']).toContain(bodyBg);

    if (bodyBg === 'rgba(0, 0, 0, 0)') {
      const rootBg = await page.evaluate(
        () => getComputedStyle(document.documentElement).backgroundColor
      );
      expect(rootBg).toBe('rgb(255, 255, 255)');
    }
  });
});
