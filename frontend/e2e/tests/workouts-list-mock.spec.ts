// Acceptance Test for Bug 010
// Description: /workouts must show a Sessions header, a summary strip
// (minutes / calories / points), day-group eyebrows, and per-row equipment
// icon tile + meta row + points badge — per docs/mocks/workouts.html.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse {
  accessToken: string;
}

test.describe('Workouts list mock alignment', () => {
  test('renders header, summary strip, day groups, icon + meta + points per row', async ({
    page,
    request
  }) => {
    const email = `wl010-${Date.now()}@forgefit.app`;
    const register = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'WL', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(register.ok()).toBeTruthy();
    const auth = (await register.json()) as AuthResponse;

    // Seed at least one session so the rows render (instead of empty state).
    const seed = await request.post(`${API_BASE}/api/sessions`, {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      data: {
        equipment: 'Treadmill',
        startedAt: new Date().toISOString(),
        durationMinutes: 22,
        distanceMiles: 2.0,
        avgHeartRateBpm: 140,
        activeCalories: 240
      },
      ignoreHTTPSErrors: true
    });
    expect(seed.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/workouts');
    await page.waitForLoadState('networkidle');

    // Page header + subtitle
    await expect(page.getByTestId('workout-list-page-title')).toBeVisible();
    await expect(page.getByTestId('workout-list-page-subtitle')).toBeVisible();

    // Summary strip with three tiles
    const summary = page.getByTestId('workout-list-summary');
    await expect(summary).toBeVisible();
    await expect(summary.getByTestId('workout-list-summary-minutes')).toBeVisible();
    await expect(summary.getByTestId('workout-list-summary-calories')).toBeVisible();
    await expect(summary.getByTestId('workout-list-summary-points')).toBeVisible();

    // At least one day-group header
    await expect(page.getByTestId('workout-list-day-group').first()).toBeVisible();

    // Per-row anatomy: equipment icon tile, meta row, points stack
    const firstRow = page.locator('[data-testid^="workout-list-row-"]').first();
    await expect(firstRow.getByTestId('workout-list-row-icon')).toBeVisible();
    await expect(firstRow.getByTestId('workout-list-row-meta')).toBeVisible();
    await expect(firstRow.getByTestId('workout-list-row-points')).toBeVisible();
  });
});
