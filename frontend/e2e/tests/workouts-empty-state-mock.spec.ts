// Acceptance Test for Bug 017
// Description: When /workouts has zero sessions, render the empty-state mock:
// hero illustration with "Day 1" badge, title, sub, two CTAs, equipment
// picker grid (4 tiles), and a Pro tip card.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Workouts empty state', () => {
  test('renders hero, badge, two CTAs, equipment picker grid, and Pro tip', async ({
    page,
    request
  }) => {
    const email = `wempty-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'WE', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();
    // intentionally no seeded sessions

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/workouts');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('workouts-empty-hero')).toBeVisible();
    await expect(page.getByTestId('workouts-empty-badge')).toBeVisible();
    await expect(page.getByTestId('workouts-empty-title')).toBeVisible();
    await expect(page.getByTestId('workouts-empty-log-cta')).toBeVisible();
    await expect(page.getByTestId('workouts-empty-home-cta')).toBeVisible();

    const pickerTiles = page.locator('[data-testid="workouts-empty-equipment-tile"]');
    expect(await pickerTiles.count()).toBeGreaterThanOrEqual(4);

    await expect(page.getByTestId('workouts-empty-pro-tip')).toBeVisible();
  });
});
