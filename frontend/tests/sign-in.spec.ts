// Acceptance Test
// Traces to: L2-002 (sign-in), L2-013 (dashboard summary), L2-044 (health endpoint)
// Description: The sample flow signs in against the live backend MVP, lands on the
//   dashboard, and renders the user's email/role plus the health-badge value fetched
//   from GET /health. Verifies the api/components/domain library separation works in
//   practice (sign-in form in `domain` consumes IAuthService from `api`; the card it
//   renders comes from `components`; the dashboard's health badge consumes
//   IHealthService from `api`).

import { expect, test } from '@playwright/test';
import { DashboardPage } from './pom/dashboard.page';
import { SignInPage } from './pom/sign-in.page';

const TEST_EMAIL = `mf1-${Date.now()}@forgefit.app`;
const TEST_PASSWORD = 'ForgeFit!2026';
const API_BASE = 'https://localhost:5001';

test.beforeAll(async ({ request }) => {
  const response = await request.post(`${API_BASE}/api/auth/register`, {
    data: {
      email: TEST_EMAIL,
      firstName: 'MF1',
      lastName: 'Acceptance',
      password: TEST_PASSWORD
    },
    ignoreHTTPSErrors: true
  });
  expect(response.ok()).toBeTruthy();
});

test('signs in and renders backend data on the dashboard', async ({ page }) => {
  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);

  await signIn.goto();
  await signIn.signIn(TEST_EMAIL, TEST_PASSWORD);

  await dashboard.waitForLoad();

  await expect(dashboard.greeting).toContainText(TEST_EMAIL);
  await expect(dashboard.greeting).toContainText('User');

  await expect(dashboard.healthBadge).toBeVisible();
  await expect(dashboard.healthBadge).toContainText('Healthy');
});
