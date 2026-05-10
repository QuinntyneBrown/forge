// Acceptance Test
// Traces to: L2-002 (sign-in), L2-013 (dashboard summary)
// Description: The sample flow signs in against the live backend MVP and lands on
//   the dashboard, where the user's email/role appear in the greeting. Verifies the
//   api/components/domain library separation works end-to-end (sign-in form in
//   `domain` consumes IAuthService from `api`; the card it renders comes from
//   `components`; the dashboard greeting projects the CurrentUser DTO from
//   IMeService).
//
// Health badge assertion was removed when FT-024 rewrote the dashboard to host
// the five new cards (daily ring, streak, weight progress, tier, leaderboard);
// health badge moves to /error-state per FT-033.

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
});
