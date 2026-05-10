// Acceptance Test
// Traces to: FT-024, L2-011..L2-014, L2-022, L2-027
// Description: A signed-in user with seeded sessions sees all five dashboard
// cards on /dashboard: today's calorie ring, streak card, weight progress,
// tier, and leaderboard. The page composes <forge-app-shell> + the five
// domain cards backed by IDashboardService / IRewardsService /
// ILeaderboardService.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard', () => {
  test('renders all five cards for a freshly-registered user', async ({ page, request }) => {
    const email = `dashboard-${Date.now()}@forgefit.app`;
    const register = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'Dash', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(register.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);

    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    // All five cards mount.
    await expect(dashboard.dailyRingCard).toBeVisible();
    await expect(dashboard.streakCard).toBeVisible();
    await expect(dashboard.weightProgressCard).toBeVisible();
    await expect(dashboard.tierCard).toBeVisible();
    await expect(dashboard.leaderboardCard).toBeVisible();

    // Daily ring shows today's calorie tally vs the default 1500 kcal target.
    await expect(dashboard.dailyRingValue).toBeVisible();
    await expect(dashboard.dailyRingMinutes).toContainText('60 min today');

    // A new user has no sessions, so the streak is 0 and the multiplier badge
    // shows the floor multiplier ×1.00.
    await expect(dashboard.streakDays).toContainText('0');
    await expect(dashboard.streakMultiplier).toContainText('×1.00');

    // Default goal is 20 lb / month and there are no weight entries yet.
    await expect(dashboard.weightProgressGoal).toContainText('20 lb / month');
    await expect(dashboard.weightProgressMtd).toContainText('0.0 lb so far');

    // Default tier for a brand-new user is Iron with 0 lifetime points.
    await expect(dashboard.tierName).toContainText('Iron');
    await expect(dashboard.tierBalance).toContainText('0 pts available');

    // Leaderboard always includes the caller; no other opted-in users yet, so
    // the list shows exactly one row (the caller).
    await expect(dashboard.leaderboardCard).toContainText('Dash Test');
  });
});
