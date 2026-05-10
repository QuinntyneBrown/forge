// Acceptance Test for Bug 030
// Description: /workouts/:id Points breakdown should expose at least the
// Base + Morning bonus + Streak multiplier rows (each with a leading icon)
// and a highlighted "Total earned" pill.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse { accessToken: string }
interface CreatedSession { id: string }

test.describe('Workout detail points breakdown', () => {
  test('renders 3+ breakdown rows with icons and a Total earned pill', async ({ page, request }) => {
    const email = `wpb-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'P', lastName: 'B', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();
    const auth = (await reg.json()) as AuthResponse;

    // Seed an early-morning treadmill session so the morning bonus row
    // should fire.
    const earlyMorning = new Date();
    earlyMorning.setHours(5, 12, 0, 0);
    const seed = await request.post(`${API_BASE}/api/sessions`, {
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      data: {
        equipment: 'Treadmill',
        startedAt: earlyMorning.toISOString(),
        durationMinutes: 22,
        distanceMiles: 2.1,
        avgHeartRateBpm: 128,
        activeCalories: 218
      },
      ignoreHTTPSErrors: true
    });
    const sessionId = ((await seed.json()) as CreatedSession).id;

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');

    await page.goto(`/workouts/${sessionId}`);
    await page.waitForLoadState('networkidle');

    const rows = page.locator('[data-testid="workout-points-breakdown-row"]');
    expect(await rows.count()).toBeGreaterThanOrEqual(3);

    // Each row has a leading icon glyph.
    for (let i = 0; i < (await rows.count()); i++) {
      const icon = rows
        .nth(i)
        .locator('.material-icons, .material-symbols-rounded')
        .first();
      await expect(icon, `row ${i} icon`).toBeVisible();
    }

    // Total earned pill present and highlighted.
    const total = page.getByTestId('workout-points-breakdown-total-pill');
    await expect(total).toBeVisible();
    await expect(total).toContainText(/total earned/i);
    await expect(total).toContainText(/\+\s*\d+\s*pts/i);
  });
});
