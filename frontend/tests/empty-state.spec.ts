// Acceptance Test
// Traces to: FT-034, L2-028, L2-029
// Description: A brand-new account that has never logged a session sees the
// EmptyState on /workouts with a primary CTA. Visiting an unmatched route
// renders NotFoundPage.

import { expect, test } from '@playwright/test';
import { SignInPage } from './pom/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test('workouts shows the empty state for a fresh account', async ({ page, request }) => {
  const email = `empty-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'Empty', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();

  const signIn = new SignInPage(page);
  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await page.waitForURL('**/dashboard');

  await page.goto('/workouts');
  await page.getByTestId('workout-list-empty').waitFor();
  await expect(page.getByTestId('workout-list-empty')).toContainText('No sessions yet');
  await expect(page.getByTestId('workout-list-empty')).toContainText('Log your first session');
});

test('unknown route renders NotFoundPage', async ({ page }) => {
  await page.goto('/does-not-exist');
  await page.getByTestId('not-found').waitFor();
  await expect(page.getByTestId('not-found')).toContainText('Page not found');
  await expect(page.getByTestId('not-found-home')).toBeVisible();
});
