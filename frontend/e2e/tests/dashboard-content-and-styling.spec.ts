// Acceptance Test for Bug 018
// Description: The /dashboard page must render the rich content and color
// treatment shown in docs/mocks/dashboard.html — beyond the placeholder
// outer cards Bug 004 covered. Specifically:
//   - Hero card uses the dark teal gradient (linear-gradient with the
//     primary teal token), not a flat tinted color.
//   - "Eating window" card is present and reflects the user profile times.
//   - "Today's sessions" card lists at least one row when the user has any
//     workouts logged today (the dev seed always has one).
//   - A horizontally-scrolling badge row is present with multiple chips.
//   - A sparkline (svg) is present in the streak/rewards card.
//   - The two hero secondary CTAs ("Start morning workout" and "View
//     today's sessions") are visible.
//
// Uses the seeded dev account so today-filtered cards are guaranteed
// non-empty and the user has profile times set.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';
const DESKTOP_VIEWPORT = { width: 1280, height: 900 };

test.describe('Bug 018: dashboard content + styling matches mock', () => {
  test.use({ viewport: DESKTOP_VIEWPORT });

  test.beforeEach(async ({ page }) => {
    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
    await dashboard.waitForLoad();
  });

  test('hero card has a dark-teal linear-gradient background', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.hero).toBeVisible();
    const bg = await dashboard.hero.evaluate((el) => {
      const cs = getComputedStyle(el);
      return cs.backgroundImage + ' ' + cs.background;
    });
    expect(bg).toMatch(/linear-gradient/);
    // The mock uses a teal gradient (#0E5A4D / #106B5C / #1B7A6A). Computed
    // styles serialize to rgb(...). We only assert that *one* of the dark-
    // teal stops is present so the test isn't brittle on token tweaks.
    const teal = /(rgb\(\s*14\s*,\s*90\s*,\s*77\s*\))|(rgb\(\s*16\s*,\s*107\s*,\s*92\s*\))|(rgb\(\s*27\s*,\s*122\s*,\s*106\s*\))/i;
    expect(bg).toMatch(teal);
  });

  test('hero card shows the eyebrow + both secondary CTAs', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.heroEyebrow).toBeVisible();
    await expect(dashboard.heroEyebrow).toHaveText(/today's active calories/i);
    await expect(dashboard.heroStartWorkoutCta).toBeVisible();
    await expect(dashboard.heroStartWorkoutCta).toContainText(/start.*workout/i);
    await expect(dashboard.heroViewSessionsCta).toBeVisible();
    await expect(dashboard.heroViewSessionsCta).toContainText(/view.*today.*sessions/i);
  });

  test('eating-window card is present and shows the profile window times', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.eatingWindowCard).toBeVisible();
    await expect(dashboard.eatingWindowTitle).toBeVisible();
    // The default seeded dev profile uses the standard kitchen-closed window
    // (8:00 PM → 6:00 AM by default per User aggregate). At minimum the
    // title should mention an AM hour the user is fasting until.
    await expect(dashboard.eatingWindowTitle).toContainText(/fasting until|kitchen closed|eating window/i);
    await expect(dashboard.eatingWindowCard).toContainText(/AM|PM/);
  });

  test('todays-sessions card lists at least one session for today', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.todaysSessionsCard).toBeVisible();
    // Seeded dev user always has at least one session today (BuildSessionDayOffsets includes 0).
    await expect(dashboard.todaysSessionsItems.first()).toBeVisible({ timeout: 10000 });
    const count = await dashboard.todaysSessionsItems.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('badge row renders multiple badge chips', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.badgeRow).toBeVisible();
    const count = await dashboard.badgeItems.count();
    expect(count).toBeGreaterThanOrEqual(3);
  });

  test('sparkline svg is rendered in the streak/rewards card', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await expect(dashboard.sparkline).toBeVisible();
    const tag = await dashboard.sparkline.evaluate((el) => el.tagName.toLowerCase());
    expect(['svg', 'canvas']).toContain(tag);
  });
});
