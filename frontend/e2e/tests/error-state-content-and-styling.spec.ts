// Acceptance Test
// Traces to: Bug 013
// Description: The /error route renders the full mock layout —
// top banner, hero illustration, query-param-driven error-code chip,
// retry CTA, and diagnostics card with leading icons.

import { expect, test } from '@playwright/test';
import { ErrorPagePom } from '../pages/error.page';

test.describe('Bug 013 — /error matches the error-state mock', () => {
  test('top banner is present with mock copy', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto();

    const banner = errorPage.topBanner();
    await expect(banner).toBeVisible();
    await expect(banner).toContainText('Apple Watch sync paused');
    await expect(banner).toContainText('14 minutes ago');
    await expect(banner.getByRole('link', { name: /retry now/i })).toBeVisible();
  });

  test('hero illustration is present', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto();

    const hero = errorPage.heroIllustration();
    await expect(hero).toBeVisible();
    // Hero must contain at least one icon glyph (svg, img, or material icon span).
    const glyphCount = await hero.locator('svg, img, .material-icons, .material-symbols-rounded').count();
    expect(glyphCount).toBeGreaterThan(0);
  });

  test('error-code chip renders the query-param value', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto('code=500');

    const chip = errorPage.errorCodeChip();
    await expect(chip).toBeVisible();
    await expect(chip).toContainText('500');
  });

  test('error-code chip falls back to a default when no code is supplied', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto();

    const chip = errorPage.errorCodeChip();
    await expect(chip).toBeVisible();
    await expect(chip).toHaveText(/\S+/);
  });

  test('retry CTA is present, enabled, and clickable', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto('code=500');

    const retry = errorPage.retryButton();
    await expect(retry).toBeVisible();
    await expect(retry).toBeEnabled();
    await expect(retry).toContainText(/retry/i);

    // Clicking retry should not throw and should keep us on a sane URL
    // (either reload /error or navigate elsewhere).
    await retry.click();
    await page.waitForLoadState('domcontentloaded');
    expect(page.url()).toMatch(/\/(error|dashboard|sign-in)/);
  });

  test('diagnostics card lists at least four items, each with a leading icon', async ({ page }) => {
    const errorPage = new ErrorPagePom(page);
    await errorPage.goto();

    const card = errorPage.diagnosticsCard();
    await expect(card).toBeVisible();
    await expect(card).toContainText(/diagnostics/i);

    const items = errorPage.diagnosticItems();
    const count = await items.count();
    expect(count).toBeGreaterThanOrEqual(4);

    for (let i = 0; i < count; i++) {
      const row = items.nth(i);
      const iconCount = await row.locator('.material-icons, .material-symbols-rounded, svg').count();
      expect(iconCount).toBeGreaterThan(0);
    }

    // Mock-specific rows
    await expect(errorPage.diagnosticItem(/forge fit servers/i)).toBeVisible();
    await expect(errorPage.diagnosticItem(/healthkit/i)).toBeVisible();
    await expect(errorPage.diagnosticItem(/apple watch/i)).toBeVisible();
  });
});
