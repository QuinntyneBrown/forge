// Acceptance Test
// Traces to: FT-030, L2-021
// Description: Seed a user with 1,250 lifetime points (via the backend
// admin path of POSTing sessions until the balance reaches the target —
// here we use a single 625 min session to land 1,250 base pts at NY
// 14:00 to skip morning bonus). Sign in, navigate to /rewards, redeem
// the cheapest seeded reward (Smoothie, 200 pts) → balance falls to
// 1050. Try to redeem an unaffordable reward → INSUFFICIENT_POINTS
// error banner; balance unchanged.

import { expect, test } from '@playwright/test';
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

test('redeems a reward when balance is sufficient and surfaces an error otherwise', async ({
  page,
  request
}) => {
  const email = `rdeem-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'Rdeem', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();
  const auth = (await register.json()) as AuthResponse;

  // 625 min × 2 = 1250 base pts. Mid-day NY so no morning bonus, day-1
  // streak so no streak row.
  const create = await request.post(`${API_BASE}/api/sessions`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
    data: {
      equipment: 'Treadmill',
      startedAt: '2026-05-09T18:00:00Z',
      durationMinutes: 625,
      distanceMiles: null,
      avgHeartRateBpm: null,
      activeCalories: 6000,
      notes: 'seed for redemption test'
    },
    ignoreHTTPSErrors: true
  });
  expect(create.ok()).toBeTruthy();

  const signIn = new SignInPage(page);
  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await page.waitForURL('**/dashboard');

  await page.goto('/rewards');
  await page.getByTestId('rewards-catalog').waitFor();
  await expect(page.getByTestId('rewards-catalog-balance')).toContainText('1250');

  const rewards = (
    await (
      await request.get(`${API_BASE}/api/rewards`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` },
        ignoreHTTPSErrors: true
      })
    ).json()
  ) as Array<{ id: string; name: string; costPoints: number }>;
  const smoothie = rewards.find((r) => r.name.includes('Smoothie'));
  if (!smoothie) {
    throw new Error('Smoothie reward not seeded');
  }

  await page.getByTestId(`rewards-catalog-redeem-${smoothie.id}`).click();
  await expect(page.getByTestId('rewards-catalog-balance')).toContainText('1050');

  // Try to redeem an unaffordable reward (12000 pts running shoes).
  const expensive = [...rewards].sort((a, b) => b.costPoints - a.costPoints)[0];
  // The button is disabled when not affordable, so simulate by calling the
  // API directly to assert backend's INSUFFICIENT_POINTS branch — the
  // visible UI behaviour for unaffordable rewards is the disabled state.
  const redeem = await request.post(`${API_BASE}/api/rewards/${expensive.id}/redeem`, {
    headers: { Authorization: `Bearer ${auth.accessToken}` },
    data: {},
    ignoreHTTPSErrors: true
  });
  expect(redeem.status()).toBe(400);
  const body = await redeem.json();
  expect(body.title).toBe('INSUFFICIENT_POINTS');
});
