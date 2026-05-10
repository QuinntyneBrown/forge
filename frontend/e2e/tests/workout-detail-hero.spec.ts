// Acceptance Test for Bug 014
// Description: /workouts/:id (edit) must show a teal hero (eyebrow / title /
// sub / 3-up stats) and an Actions card containing Save / Duplicate /
// Delete + an info caption — per docs/mocks/workout-detail.html.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse { accessToken: string }
interface CreatedSession { id: string }

test.describe('Workout detail hero + actions', () => {
  test('renders teal hero with stats and an Actions card', async ({ page, request }) => {
    const email = `wd14-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'WD', lastName: '14', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();
    const auth = (await reg.json()) as AuthResponse;

    const seed = await request.post(`${API_BASE}/api/sessions`, {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      data: {
        equipment: 'Treadmill',
        startedAt: new Date().toISOString(),
        durationMinutes: 22,
        distanceMiles: 2.1,
        avgHeartRateBpm: 128,
        activeCalories: 218
      },
      ignoreHTTPSErrors: true
    });
    expect(seed.ok()).toBeTruthy();
    const session = (await seed.json()) as CreatedSession;

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto(`/workouts/${session.id}`);
    await page.waitForLoadState('networkidle');

    // Hero
    await expect(page.getByTestId('workout-detail-hero')).toBeVisible();
    await expect(page.getByTestId('workout-detail-hero-eyebrow')).toBeVisible();
    await expect(page.getByTestId('workout-detail-hero-title')).toBeVisible();
    const stats = page.locator('[data-testid="workout-detail-hero-stat"]');
    expect(await stats.count()).toBeGreaterThanOrEqual(3);

    // Actions card with Save / Duplicate / Delete + info caption
    const actions = page.getByTestId('workout-detail-actions-card');
    await expect(actions).toBeVisible();
    await expect(actions.getByTestId('workout-detail-duplicate')).toBeVisible();
    await expect(actions.getByTestId('workout-detail-delete')).toBeVisible();
    await expect(actions.getByTestId('workout-detail-info-caption')).toBeVisible();
  });
});
