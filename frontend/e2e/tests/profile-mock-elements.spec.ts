// Acceptance Test for Bug 011
// Description: /profile must show a hero block with avatar + name + tier
// chip, plus Goals, Windows, Integrations, and Save cards — per
// docs/mocks/profile.html.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Profile mock alignment', () => {
  test('renders hero, tier chip, and the four core cards', async ({ page, request }) => {
    const email = `prof-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'Prof', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto('/profile');
    await page.waitForLoadState('networkidle');

    // Hero
    await expect(page.getByTestId('profile-hero')).toBeVisible();
    await expect(page.getByTestId('profile-hero-avatar')).toBeVisible();
    await expect(page.getByTestId('profile-hero-name')).toBeVisible();
    await expect(page.getByTestId('profile-hero-tier')).toBeVisible();

    // Cards
    await expect(page.getByTestId('profile-account-card')).toBeVisible();
    await expect(page.getByTestId('profile-goals-card')).toBeVisible();
    await expect(page.getByTestId('profile-windows-card')).toBeVisible();
    await expect(page.getByTestId('profile-integrations-card')).toBeVisible();

    // Save card with Save changes + Sign out
    const saveCard = page.getByTestId('profile-save-card');
    await expect(saveCard).toBeVisible();
    await expect(saveCard.getByTestId('profile-save-button')).toBeVisible();
    await expect(saveCard.getByTestId('profile-sign-out-button')).toBeVisible();
  });
});
