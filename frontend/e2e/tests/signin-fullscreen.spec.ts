// Acceptance Test for Bug 007
// Description: The sign-in page must fill the full viewport on desktop — no
// trailing white space below the content. The colored hero panel must reach
// the bottom of the viewport (the page background extends to the bottom).

import { expect, test } from '@playwright/test';

test.describe('Sign-in fullscreen on desktop', () => {
  test('main fills the viewport and hero extends to the bottom', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto('/sign-in');
    await page.waitForLoadState('networkidle');

    const viewportHeight = page.viewportSize()!.height;
    const main = page.locator('main.sign-in-page');
    const mainBox = await main.boundingBox();
    expect(mainBox).not.toBeNull();
    expect(mainBox!.height).toBeGreaterThanOrEqual(viewportHeight - 1);

    // The green hero panel should reach the bottom edge of the viewport on
    // desktop (it occupies the left column of the grid).
    const heroBox = await page.locator('.sign-in-page__hero').boundingBox();
    expect(heroBox).not.toBeNull();
    const heroBottom = heroBox!.y + heroBox!.height;
    // Allow a small tolerance for the footer row at the bottom (~80px).
    expect(heroBottom).toBeGreaterThanOrEqual(viewportHeight - 200);
  });
});
