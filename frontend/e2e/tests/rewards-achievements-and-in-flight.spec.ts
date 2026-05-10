// Acceptance Test for Bug 012
// Description: /rewards must render a medal grid in "Recent achievements"
// (multiple tiles, not a single text card) and a list of in-flight progress
// rows (icon + title + counter + progress bar) — per docs/mocks/rewards.html.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Rewards achievements + in-flight', () => {
  test('renders multiple medal tiles and progress rows', async ({ page, request }) => {
    const email = `r12-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'R', lastName: '12', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/rewards');
    await page.waitForLoadState('networkidle');

    // Achievements: at least 4 medal tiles.
    const medals = page.locator('[data-testid="rewards-achievement-medal"]');
    await expect(medals.first()).toBeVisible();
    expect(await medals.count()).toBeGreaterThanOrEqual(4);

    // First medal has the round icon and a title.
    await expect(
      medals.first().locator('[data-testid="rewards-achievement-medal-icon"]')
    ).toBeVisible();
    await expect(
      medals.first().locator('[data-testid="rewards-achievement-medal-title"]')
    ).toBeVisible();

    // In-flight: at least 3 progress rows, each with an icon, counter, bar.
    const progress = page.locator('[data-testid="rewards-in-flight-row"]');
    await expect(progress.first()).toBeVisible();
    expect(await progress.count()).toBeGreaterThanOrEqual(3);

    await expect(progress.first().locator('[data-testid="rewards-in-flight-counter"]')).toBeVisible();
    await expect(progress.first().locator('[data-testid="rewards-in-flight-bar"]')).toBeVisible();
  });
});
