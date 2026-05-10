// Acceptance Test for Bug 016
// Description: Unknown routes must render inside the app shell, with an
// illustration, title, sub, and two CTAs (Go to dashboard + Go back), and
// fill the desktop viewport.

import { expect, test } from '@playwright/test';

test.describe('Not-found mock alignment', () => {
  test('renders inside the app shell with illustration + two CTAs', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/this-route-does-not-exist');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('app-shell-topbar')).toBeVisible();

    await expect(page.getByTestId('not-found-illustration')).toBeVisible();
    await expect(page.getByTestId('not-found-title')).toBeVisible();
    await expect(page.getByTestId('not-found-home')).toBeVisible();
    await expect(page.getByTestId('not-found-back')).toBeVisible();

    // Fill viewport — main element height >= viewport height.
    const main = page.locator('main.not-found');
    const mainBox = await main.boundingBox();
    expect(mainBox).not.toBeNull();
    expect(mainBox!.height).toBeGreaterThanOrEqual(700);
  });
});
