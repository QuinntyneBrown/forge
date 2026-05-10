// Acceptance Test for Bug 031
// Description: Per docs/mocks/dashboard.html (lines 121-123, 298), the
// dashboard's "+ Log workout" affordance must be a fixed-position FAB
// pinned to the bottom-right of the viewport (clear of the bottom nav)
// using the orange secondary token (#B8531A) — the SAME visual treatment
// as the workouts-list FAB shipped by Bug 026. The implementation must
// NOT render an inline pill inside the Today's sessions card header.

import { Browser, expect, Locator, Page, test } from '@playwright/test';
import { BottomNavPage } from '../pages/bottom-nav.page';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';

// Mock token: --md-sys-color-secondary: #B8531A — same orange as the
// workouts-list FAB from Bug 026.
const ORANGE_RGB = { r: 0xb8, g: 0x53, b: 0x1a };

interface Rgb {
  r: number;
  g: number;
  b: number;
}

function parseRgb(value: string): Rgb {
  const match = value.match(/(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/);
  if (!match) {
    throw new Error(`Could not parse rgb-ish value: ${value}`);
  }
  return { r: Number(match[1]), g: Number(match[2]), b: Number(match[3]) };
}

function colorDistance(a: Rgb, b: Rgb): number {
  return Math.sqrt(
    (a.r - b.r) ** 2 + (a.g - b.g) ** 2 + (a.b - b.b) ** 2
  );
}

async function signInDev(page: Page): Promise<DashboardPage> {
  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);
  await signIn.goto();
  await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
  await dashboard.waitForLoad();
  return dashboard;
}

async function newMobileContext(browser: Browser) {
  const ctx = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 390, height: 844 }
  });
  const page = await ctx.newPage();
  return { ctx, page };
}

test.describe('Bug 031: dashboard fixed orange Log workout FAB', () => {
  test('FAB exists, pinned bottom-right of the viewport with position:fixed', async ({
    page
  }) => {
    const dashboard = await signInDev(page);
    const fab = dashboard.logWorkoutFab;
    await expect(fab).toBeVisible();
    await expect(fab).toContainText(/log workout/i);

    const position = await fab.evaluate((el) => getComputedStyle(el).position);
    expect(position, 'FAB must be position:fixed (not inline)').toBe('fixed');

    const viewport = page.viewportSize();
    if (!viewport) throw new Error('Viewport size not available');

    const rect = await fab.evaluate((el) => {
      const r = el.getBoundingClientRect();
      return { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
    });

    // Within ~24px of viewport right edge.
    const rightGap = viewport.width - rect.right;
    expect(
      rightGap,
      `FAB right edge should sit within 24px of viewport right (gap=${rightGap})`
    ).toBeLessThanOrEqual(40);
    expect(rightGap).toBeGreaterThanOrEqual(0);
  });

  test('FAB background renders the orange secondary token', async ({ page }) => {
    const dashboard = await signInDev(page);
    const fab = dashboard.logWorkoutFab;
    await expect(fab).toBeVisible();

    const bg = await fab.evaluate((el) => getComputedStyle(el).backgroundColor);
    const rgb = parseRgb(bg);
    const distance = colorDistance(rgb, ORANGE_RGB);
    expect(
      distance,
      `Expected FAB background near #B8531A (orange). Got ${bg} (rgb ${rgb.r},${rgb.g},${rgb.b}; distance=${distance.toFixed(1)}). Same orange as workouts-list FAB (Bug 026).`
    ).toBeLessThan(35);
  });

  test('clicking the FAB navigates to /workouts/new', async ({ page }) => {
    const dashboard = await signInDev(page);
    const fab = dashboard.logWorkoutFab;
    await expect(fab).toBeVisible();
    await fab.click();
    await page.waitForURL('**/workouts/new');
    expect(page.url()).toMatch(/\/workouts\/new$/);
  });

  test('Today\'s sessions card has no inline Log workout pill', async ({
    page
  }) => {
    const dashboard = await signInDev(page);
    await expect(dashboard.todaysSessionsCard).toBeVisible();

    // No inline pill (button or anchor) inside the Today's sessions card.
    await expect(
      dashboard.todaysSessionsCard.getByRole('button', { name: /log workout/i })
    ).toHaveCount(0);
    await expect(
      dashboard.todaysSessionsCard.getByRole('link', { name: /log workout/i })
    ).toHaveCount(0);
  });

  test('mobile viewport: FAB is pinned bottom-right and does NOT overlap the bottom nav', async ({
    browser
  }) => {
    const { ctx, page } = await newMobileContext(browser);
    try {
      const dashboard = await signInDev(page);
      const fab = dashboard.logWorkoutFab;
      const nav = new BottomNavPage(page);

      await expect(fab).toBeVisible();
      await expect(nav.nav).toBeVisible();

      const fabRect = await fab.evaluate((el) => {
        const r = el.getBoundingClientRect();
        return { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
      });
      const navBox = await nav.navBox();

      // FAB must sit ENTIRELY above the bottom nav (allow ~4px slack for
      // shadow / focus ring artefacts).
      const overlap = fabRect.bottom - navBox.y;
      expect(
        overlap,
        `FAB bottom (${fabRect.bottom}) must be above bottom-nav top (${navBox.y}); overlap=${overlap}px`
      ).toBeLessThanOrEqual(4);

      // FAB still pinned to right edge on mobile.
      const viewport = page.viewportSize();
      if (!viewport) throw new Error('Viewport size not available');
      const rightGap = viewport.width - fabRect.right;
      expect(rightGap).toBeLessThanOrEqual(40);
      expect(rightGap).toBeGreaterThanOrEqual(0);
    } finally {
      await ctx.close();
    }
  });
});
