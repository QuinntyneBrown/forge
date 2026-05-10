// Acceptance Test for Bug 005
// Description: The /workouts/new page should reflect the visual structure of
// docs/mocks/workout-detail.html — a hero header (with eyebrow + title), a
// "Session details" card containing the form, and an actions row with Save +
// Cancel.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Workout new mock alignment', () => {
  test('renders hero, session-details card, and Save/Cancel actions', async ({
    page,
    request
  }) => {
    const email = `wnew-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'WN', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    // Persist the session across the hard navigation to /workouts/new — the
    // in-memory token would otherwise drop and the auth guard would bounce.
    await page.getByTestId('sign-in-remember-me').click();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    await page.goto('/workouts/new');

    const hero = page.getByTestId('workout-new-hero');
    await expect(hero).toBeVisible();
    await expect(page.getByTestId('workout-new-hero-eyebrow')).toBeVisible();
    await expect(page.getByTestId('workout-new-hero-title')).toBeVisible();

    const detailsCard = page.getByTestId('workout-new-session-details-card');
    await expect(detailsCard).toBeVisible();
    await expect(detailsCard).toContainText(/session details/i);

    // Form fields are inside the session details card.
    await expect(
      detailsCard.getByTestId('workout-detail-form-equipment')
    ).toBeVisible();

    const cancelBtn = page.getByTestId('workout-new-cancel');
    await expect(cancelBtn).toBeVisible();
    await expect(cancelBtn).toContainText(/cancel/i);

    // Save button keeps the existing form-level testid.
    await expect(page.getByTestId('workout-detail-form-submit')).toBeVisible();
  });
});
