// Acceptance Test for Bug 022
// Description: Dashboard hero title must read "You're {N} cal from your daily
// goal" and ship a 3-row stat list (Workout minutes / Avg HR / Weight trend);
// streak card must show "{N}-day morning streak" with 2-up mini stats
// (Reward points today / Total points balance + Redeem link).

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard hero + streak copy', () => {
  test('hero title and stat list, streak card title and mini-stat grid', async ({
    page,
    request
  }) => {
    const email = `dh22-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'D', lastName: '22', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('networkidle');

    // Hero title
    const heroTitle = page.getByTestId('dashboard-hero-title');
    await expect(heroTitle).toBeVisible();
    await expect(heroTitle).toHaveText(/cal from your daily goal/i);

    // Hero stat list — 3 rows
    const stats = page.locator('[data-testid="dashboard-hero-stat"]');
    expect(await stats.count()).toBe(3);
    await expect(page.getByTestId('dashboard-hero-stat-minutes')).toBeVisible();
    await expect(page.getByTestId('dashboard-hero-stat-hr')).toBeVisible();
    await expect(page.getByTestId('dashboard-hero-stat-weight')).toBeVisible();

    // Streak card
    const streakTitle = page.getByTestId('dashboard-streak-title');
    await expect(streakTitle).toBeVisible();
    await expect(streakTitle).toHaveText(/morning streak/i);

    await expect(page.getByTestId('dashboard-streak-points-today')).toBeVisible();
    await expect(page.getByTestId('dashboard-streak-points-total')).toBeVisible();
    await expect(page.getByTestId('dashboard-streak-redeem-link')).toBeVisible();
  });
});
