// Acceptance Test
// Traces to: FT-027, L2-007, L2-008, L2-010, L2-028
// Description: A signed-in user with mixed-equipment sessions seeded
// server-side opens /workouts, sees the chip filters and the rows, and
// filtering by Treadmill narrows the list to treadmill rows only.

import { expect, test } from '@playwright/test';
import { SignInPage } from './pom/sign-in.page';
import { WorkoutsListPage } from './pom/workouts-list.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  role: string;
}

test('workouts list filters to treadmill rows when the Treadmill chip is selected', async ({ page, request }) => {
  const email = `wlist-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'WList', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();
  const auth = (await register.json()) as AuthResponse;

  const equipmentMix = ['Treadmill', 'Treadmill', 'IndoorBike', 'BenchPress', 'Elliptical'];
  const startedAtBase = new Date('2026-05-09T18:00:00Z').getTime();
  for (let i = 0; i < equipmentMix.length; i++) {
    const startedAt = new Date(startedAtBase - i * 60_000).toISOString();
    const seed = await request.post(`${API_BASE}/api/sessions`, {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      data: {
        equipment: equipmentMix[i],
        startedAt,
        durationMinutes: 22,
        distanceMiles: 2.0,
        avgHeartRateBpm: 140,
        activeCalories: 240,
        notes: `seed ${i}`
      },
      ignoreHTTPSErrors: true
    });
    expect(seed.ok()).toBeTruthy();
  }

  const signIn = new SignInPage(page);
  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await page.waitForURL('**/dashboard');

  const list = new WorkoutsListPage(page);
  await list.goto();

  await expect(list.rows).toBeVisible();
  expect(await list.rowCount()).toBe(5);

  await list.equipmentChip('Treadmill').click();
  await expect.poll(() => list.rowCount()).toBe(2);
  const text = await list.rows.innerText();
  expect(text).not.toContain('IndoorBike');
  expect(text).not.toContain('BenchPress');
  expect(text).not.toContain('Elliptical');
});
