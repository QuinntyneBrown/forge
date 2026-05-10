// Acceptance Test for Bug 010
// Description: The /workouts page must render the rich content from
// docs/mocks/workouts.html — beyond what the workouts-list-mock smoke spec
// covers. Specifically:
//   - Summary strip with the four mock stats (sessions in subtitle plus
//     minutes / calories / points tiles).
//   - At least 2 day-group eyebrow headers visible (the dev seed has
//     sessions across many days, including today and yesterday).
//   - First row's equipment icon tile carries an accessibility label
//     naming the equipment.
//   - First row's meta strip has icon-prefixed entries for duration,
//     calories, and at least one secondary metric (distance / HR).
//   - First row's right stack shows "+N pts" and a time-of-day caption.
//
// Uses the seeded dev account so day grouping, summary totals, and
// per-row metrics are deterministic.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';
import { WorkoutsListPage } from '../pages/workouts-list.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';
const DESKTOP_VIEWPORT = { width: 1280, height: 900 };

test.describe('Bug 010: workouts list content + styling matches mock', () => {
  test.use({ viewport: DESKTOP_VIEWPORT });

  test.beforeEach(async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
    await page.waitForURL('**/dashboard');

    const list = new WorkoutsListPage(page);
    await list.goto();
    await page.waitForLoadState('networkidle');
  });

  test('summary strip exposes the four mock stats (subtitle + 3 tiles)', async ({ page }) => {
    const list = new WorkoutsListPage(page);
    await expect(list.pageTitle).toBeVisible();
    await expect(list.pageTitle).toHaveText(/sessions/i);
    await expect(list.pageSubtitle).toBeVisible();
    // The mock subtitle reads "12 sessions · 6 h 14 min · 4,820 cal this week".
    // Assert the dev seed produces a subtitle with a non-zero session count.
    await expect(list.pageSubtitle).toContainText(/\d+\s+session/i);

    await expect(list.summaryStrip).toBeVisible();
    await expect(list.summaryStat('minutes')).toBeVisible();
    await expect(list.summaryStat('minutes')).toContainText(/min/i);
    await expect(list.summaryStat('calories')).toBeVisible();
    await expect(list.summaryStat('calories')).toContainText(/\d/);
    await expect(list.summaryStat('points')).toBeVisible();
    await expect(list.summaryStat('points')).toContainText(/\+?\d/);
  });

  test('at least two day-group headers are visible', async ({ page }) => {
    const list = new WorkoutsListPage(page);
    await expect(list.dayGroups.first()).toBeVisible();
    const groupCount = await list.dayGroups.count();
    expect(groupCount).toBeGreaterThanOrEqual(2);

    const headerCount = await list.dayGroupHeaders().count();
    expect(headerCount).toBeGreaterThanOrEqual(2);

    // First group should be Today (dev seed always logs at offset 0).
    const firstHeader = list.dayGroupHeaders().first();
    await expect(firstHeader).toBeVisible();
    await expect(firstHeader).toContainText(/today/i);
  });

  test('first session row has an equipment icon tile with a meaningful aria-label', async ({ page }) => {
    const list = new WorkoutsListPage(page);
    const row = list.sessionRow(0);
    await expect(row).toBeVisible();
    const iconTile = list.sessionRowEquipmentIcon(row);
    await expect(iconTile).toBeVisible();

    // The tile should contain a glyph (svg or material-icons span).
    const glyphCount = await iconTile.locator('svg, .material-icons, .material-symbols-rounded, mat-icon').count();
    expect(glyphCount).toBeGreaterThanOrEqual(1);

    // And the tile must announce the equipment to assistive tech.
    const ariaLabel = await iconTile.getAttribute('aria-label');
    expect(ariaLabel).not.toBeNull();
    expect(ariaLabel!.trim()).not.toBe('');
    expect(ariaLabel!.toLowerCase()).toMatch(/treadmill|bike|bench|elliptical/);
  });

  test('first session row meta strip has icon-prefixed duration, calories, and a secondary metric', async ({ page }) => {
    const list = new WorkoutsListPage(page);
    const row = list.sessionRow(0);

    const duration = list.sessionRowMetaIcon(row, 'duration');
    await expect(duration).toBeVisible();
    await expect(duration).toContainText(/min/i);
    expect(await duration.locator('.material-icons, .material-symbols-rounded, mat-icon, svg').count())
      .toBeGreaterThanOrEqual(1);

    const calories = list.sessionRowMetaIcon(row, 'calories');
    await expect(calories).toBeVisible();
    await expect(calories).toContainText(/cal/i);
    expect(await calories.locator('.material-icons, .material-symbols-rounded, mat-icon, svg').count())
      .toBeGreaterThanOrEqual(1);

    // Treadmill / bike rows should expose a distance pill; bench should
    // expose HR / sets. The dev seed's first row is unspecified, so we
    // accept either.
    const distance = list.sessionRowMetaIcon(row, 'distance');
    const hr = list.sessionRowMetaIcon(row, 'hr');
    const distanceCount = await distance.count();
    const hrCount = await hr.count();
    expect(distanceCount + hrCount).toBeGreaterThanOrEqual(1);
  });

  test('first session row right stack shows +N pts and a time caption', async ({ page }) => {
    const list = new WorkoutsListPage(page);
    const row = list.sessionRow(0);

    const points = list.sessionRowPoints(row);
    await expect(points).toBeVisible();
    await expect(points).toHaveText(/^\+\d+\s*pts$/i);

    const time = list.sessionRowTime(row);
    await expect(time).toBeVisible();
    // Match either 12-hour ("5:12 AM") or 24-hour ("17:12") locale formats.
    await expect(time).toHaveText(/\d{1,2}:\d{2}(\s?(AM|PM))?/i);
  });
});
