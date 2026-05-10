// Acceptance Test for Bug 015
// Description: At mobile viewport (390x844) the fixed bottom navigation
// must NOT cover the last meaningful piece of page content on any
// authenticated screen that displays the bottom-nav. Verified by:
//
//   1. Architectural assertion — the app-shell main scroll container
//      reserves bottom space at least the height of the bottom nav,
//      via the global `--bottom-nav-safe-area` token. This is the
//      durable invariant: page authors should not have to remember to
//      add `padding-bottom: 96px` on every new page.
//   2. User-facing assertion — for /dashboard, /workouts, /rewards,
//      after scrolling to the very bottom the last meaningful card /
//      row sits fully above the bottom nav (not clipped).
//
// Uses the seeded dev account (lots of data so the lists overflow).

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
  test('app shell reserves bottom-nav-safe-area on the main scroll container', async ({
    browser
  }) => {
    const { ctx, page } = await signInAsDev(browser);
    const nav = new BottomNavPage(page);
    await expect(nav.nav).toBeVisible();

    // Architectural invariant: the shell main reserves vertical space
    // for the bottom nav. The nav is ~64px tall; we require the main
    // container to inset its content by at least that much so that
    // pages do not have to repeat `padding-bottom: 96px` boilerplate.
    const navHeight = (await nav.navBox()).height;
    const mainPaddingBottom = await page.evaluate(() => {
      const main = document.querySelector<HTMLElement>('.app-shell__main');
      if (!main) {
        return -1;
      }
      const cs = getComputedStyle(main);
      return parseFloat(cs.paddingBottom) || 0;
    });
    expect(mainPaddingBottom).toBeGreaterThanOrEqual(navHeight);

    // The global token should also be defined so individual pages can
    // opt-in if they have their own scroll container.
    const tokenValue = await page.evaluate(() => {
      const cs = getComputedStyle(document.documentElement);
      return cs.getPropertyValue('--bottom-nav-safe-area').trim();
    });
    expect(tokenValue).not.toBe('');

    await ctx.close();
  });

  test('dashboard: last card (leaderboard) is fully visible above the bottom nav', async ({
    browser
  }) => {
    const { ctx, page } = await signInAsDev(browser);
    const nav = new BottomNavPage(page);
    await expect(nav.nav).toBeVisible();

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
