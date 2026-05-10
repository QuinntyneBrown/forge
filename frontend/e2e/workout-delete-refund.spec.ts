// Acceptance Test
// Traces to: FT-029, L2-009, L2-018, L2-019, L2-020
// Description: Delete a session from /workouts/:id; the dashboard balance
// returns to the pre-session value because BT-012 wrote a compensating
// Refund ledger row.

import { expect, test } from '@playwright/test';
import { DashboardPage } from './pom/dashboard.page';
import { SignInPage } from './pom/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  role: string;
}

test('deleting a session refunds points and returns the dashboard balance to zero', async ({
  page,
  request
}) => {
  const email = `wdel-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'WDel', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();
  const auth = (await register.json()) as AuthResponse;

  // Seed a 22-min treadmill session at NY 14:00 (no morning bonus, day 1
  // streak = 0). After scoring this is +44 Base.
  const startedAt = '2026-05-09T18:00:00Z';
  const create = await request.post(`${API_BASE}/api/sessions`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
    data: {
      equipment: 'Treadmill',
      startedAt,
      durationMinutes: 22,
      distanceMiles: 2.1,
      avgHeartRateBpm: 128,
      activeCalories: 218,
      notes: 'to delete'
    },
    ignoreHTTPSErrors: true
  });
  expect(create.ok()).toBeTruthy();
  const session = (await create.json()) as { id: string };

  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);

  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await dashboard.waitForLoad();

  // Tier balance reads 44 pts available before deletion.
  await expect(dashboard.tierBalance).toContainText('44 pts available');

  // Open the detail page and delete.
  await page.goto(`/workouts/${session.id}`);
  await page.getByTestId('workout-points-breakdown').waitFor();
  await page.getByTestId('workout-detail-delete').click();

  // Routed back to dashboard; balance reflects the Refund row.
  await dashboard.waitForLoad();
  await expect(dashboard.tierBalance).toContainText('0 pts available');
});
