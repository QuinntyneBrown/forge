// Acceptance Test for Bug 026
// Description: /workouts header should drop the "New session" pill in favor
// of a fixed orange "+ Log workout" FAB pinned bottom-right.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Workouts list FAB', () => {
  test('renders an orange Log workout FAB and removes the New session pill', async ({
    page,
    request
  }) => {
    const email = `wfab-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'W', lastName: 'F', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/workouts');
    await page.waitForLoadState('networkidle');

    // FAB present
    const fab = page.getByTestId('workouts-list-log-workout-fab');
    await expect(fab).toBeVisible();
    await expect(fab).toContainText(/log workout/i);

    // No "New session" pill in the header anymore
    await expect(page.getByTestId('workout-list-new')).toHaveCount(0);
  });
});
