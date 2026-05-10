// Acceptance Test for Bug 029
// Description: Each /rewards catalog row should lead with a 48x48 colored
// icon tile containing a Material Symbols glyph instead of an empty
// progress-ring outline.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Rewards catalog icon tiles', () => {
  test('every catalog row renders an icon tile with a glyph', async ({ page, request }) => {
    const email = `rcat-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'R', lastName: 'C', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/rewards');
    await page.waitForLoadState('networkidle');

    // At least one catalog row + every row has an icon tile.
    // Use the structural class to avoid matching nested testids that share
    // the rewards-catalog-row- prefix (e.g. rewards-catalog-row-icon).
    const rows = page.locator('li.rewards-catalog__row');
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThanOrEqual(1);

    for (let i = 0; i < rowCount; i++) {
      const row = rows.nth(i);
      const tile = row.locator('[data-testid="rewards-catalog-row-icon"]');
      await expect(tile, `row ${i} icon tile`).toBeVisible();
      const text = (await tile.locator('.material-icons, .material-symbols-rounded').first().textContent()) ?? '';
      expect(text.trim().length, `row ${i} icon glyph text`).toBeGreaterThan(0);
    }
  });
});
