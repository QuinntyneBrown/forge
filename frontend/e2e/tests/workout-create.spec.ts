// Acceptance Test
// Traces to: FT-028, L2-007, L2-009
// Description: A signed-in user opens /workouts/new, fills the detail form,
// submits, and lands on the dashboard. The newly-created session shows up in
// the daily-ring contribution because the dashboard query hits BT-029
// freshly on navigation.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test('creates a treadmill session and lands on the dashboard', async ({ page, request }) => {
  const email = `wnew-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'WNew', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();

  const signIn = new SignInPage(page);
  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await page.waitForURL('**/dashboard');

  await page.goto('/workouts/new');
  await page.getByTestId('workout-detail-form').waitFor();

  // Equipment defaults to Treadmill; just supply duration / distance / HR / calories.
  await page.getByTestId('workout-detail-form-duration').fill('22');
  await page.getByTestId('workout-detail-form-distance').fill('2.1');
  await page.getByTestId('workout-detail-form-hr').fill('128');
  await page.getByTestId('workout-detail-form-calories').fill('218');

  await page.getByTestId('workout-detail-form-submit').click();
  await page.waitForURL('**/dashboard');

  const dashboard = new DashboardPage(page);
  await dashboard.dailyRingValue.waitFor();
  await expect(dashboard.dailyRingValue).toContainText('218');
});
