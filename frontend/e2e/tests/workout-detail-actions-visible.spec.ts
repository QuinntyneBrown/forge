// Acceptance Test for Bug 024
// Description: The Actions card on /workouts/:id must show two buttons whose
// labels are actually visible to the user — both have non-empty textContent
// AND their resolved text color is not the same as the resolved background
// color (catches the "white-on-white invisible label" failure mode).

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';
import { WorkoutsListPage } from '../pages/workouts-list.page';
import { WorkoutDetailPage } from '../pages/workout-detail.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';
const DESKTOP_VIEWPORT = { width: 1280, height: 900 };

test.describe('Bug 024: workout-detail Actions card buttons render labels visibly', () => {
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

  test('Duplicate and Delete buttons are visible and their labels are readable', async ({
    page
  }) => {
    // Click into the first workout row to land on /workouts/:id.
    const firstRow = page.locator('[data-testid^="workout-list-row-"]').first();
    await expect(firstRow).toBeVisible();
    await firstRow.click();
    await page.waitForURL(/\/workouts\/[0-9a-fA-F-]+$/);

    const detail = new WorkoutDetailPage(page);
    await expect(detail.actionsCard).toBeVisible();

    // Duplicate button: visible, labelled "Duplicate".
    await expect(detail.duplicateButton).toBeVisible();
    await expect(detail.duplicateButton).toContainText(/duplicate/i);

    // Delete button: visible, labelled containing "Delete".
    await expect(detail.deleteButton).toBeVisible();
    await expect(detail.deleteButton).toContainText(/delete/i);

    // Each button's projected text must actually be readable — its resolved
    // color must not equal its resolved background color (catches the
    // white-on-white failure mode).
    for (const testId of ['workout-detail-duplicate', 'workout-detail-delete']) {
      const colors = await page.evaluate((id) => {
        const el = document.querySelector(`[data-testid="${id}"]`) as HTMLElement | null;
        if (!el) {
          return { found: false, color: '', background: '', rect: null };
        }
        const cs = getComputedStyle(el);
        // Walk up to find the first non-transparent background ancestor for a
        // fair comparison — buttons commonly have transparent bg.
        let bg = cs.backgroundColor;
        let ancestor: HTMLElement | null = el.parentElement;
        const isTransparent = (c: string) =>
          c === 'rgba(0, 0, 0, 0)' || c === 'transparent' || c === '';
        while (isTransparent(bg) && ancestor) {
          bg = getComputedStyle(ancestor).backgroundColor;
          ancestor = ancestor.parentElement;
        }
        const rect = el.getBoundingClientRect();
        return {
          found: true,
          color: cs.color,
          background: bg,
          rect: { width: rect.width, height: rect.height }
        };
      }, testId);

      expect(colors.found, `${testId} present in DOM`).toBe(true);
      expect(colors.rect!.width, `${testId} has non-zero width`).toBeGreaterThan(0);
      expect(colors.rect!.height, `${testId} has non-zero height`).toBeGreaterThan(0);
      expect(
        colors.color,
        `${testId} text color (${colors.color}) must differ from background (${colors.background})`
      ).not.toBe(colors.background);
    }
  });
});
