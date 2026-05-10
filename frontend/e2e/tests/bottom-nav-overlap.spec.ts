// Acceptance Test for Bug 015
// Description: At mobile viewport (390x844) the fixed bottom navigation
// must NOT cover the last meaningful piece of page content on any
// authenticated screen that displays the bottom-nav. The expected
// behaviour is that the page reserves at least the nav's height
// (~64px + safe-area) of bottom padding so that scrolling all the way
// down keeps the last card / form field visible above the nav.
//
// Routes covered:
//   /dashboard  → last card is the Leaderboard card
//   /workouts   → last visible row in the workout list
//   /rewards    → last visible row in the rewards catalog
//
// Uses the seeded dev account so that the lists genuinely overflow.

import { expect, test } from '@playwright/test';
import { BottomNavPage } from '../pages/bottom-nav.page';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';
const MOBILE_VIEWPORT = { width: 390, height: 844 };

async function signInAsDev(browser: import('@playwright/test').Browser) {
  const ctx = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: MOBILE_VIEWPORT
  });
  const page = await ctx.newPage();
  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);
  await signIn.goto();
  await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
  await dashboard.waitForLoad();
  return { ctx, page };
}

test.describe('Bug 015: bottom nav must not cover page content on mobile', () => {
  test('dashboard: last card (leaderboard) is fully visible above the bottom nav', async ({
    browser
  }) => {
    const { ctx, page } = await signInAsDev(browser);
    const nav = new BottomNavPage(page);
    await expect(nav.nav).toBeVisible();

    // Dashboard last card per docs/mocks/dashboard.html is the
    // leaderboard card.
    const lastCard = page.getByTestId('leaderboard-card');
    await expect(lastCard).toBeAttached();
    await nav.scrollToBottom();
    await nav.expectNotCoveredByNav(lastCard);

    await ctx.close();
  });

  test('workouts: last session row is fully visible above the bottom nav', async ({
    browser
  }) => {
    const { ctx, page } = await signInAsDev(browser);
    const nav = new BottomNavPage(page);

    await page.goto('/workouts');
    await page.getByTestId('workout-list').waitFor();
    await page.waitForLoadState('networkidle');
    await expect(nav.nav).toBeVisible();

    const rows = page.locator('[data-testid^="workout-list-row-"]');
    await expect(rows.first()).toBeVisible();
    const lastRow = rows.last();

    await nav.scrollToBottom();
    await nav.expectNotCoveredByNav(lastRow);

    await ctx.close();
  });

  test('rewards: last catalog row is fully visible above the bottom nav', async ({
    browser
  }) => {
    const { ctx, page } = await signInAsDev(browser);
    const nav = new BottomNavPage(page);

    await page.goto('/rewards');
    await page.getByTestId('rewards-catalog').waitFor();
    await page.waitForLoadState('networkidle');
    await expect(nav.nav).toBeVisible();

    const rows = page.locator('[data-testid^="rewards-catalog-row-"]');
    await expect(rows.first()).toBeVisible();
    const lastRow = rows.last();

    await nav.scrollToBottom();
    await nav.expectNotCoveredByNav(lastRow);

    await ctx.close();
  });
});
