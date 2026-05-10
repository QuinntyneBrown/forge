// Acceptance Test for Bug 023
// Description: Bottom nav must be glued to the bottom edge of the viewport on
// every authenticated route below 1100px (mobile + tablet). The dashboard
// already passes; the regression is on workouts/new, workout-detail, profile,
// rewards.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

interface AuthResponse { accessToken: string }
interface CreatedSession { id: string }

test.describe('Bottom nav pinned across routes', () => {
  for (const viewport of [
    { name: 'mobile', width: 390, height: 740 },
    { name: 'tablet', width: 834, height: 1024 }
  ]) {
    test(`stays pinned on workouts/new at ${viewport.name}`, async ({ page, request }) => {
      const email = `bn23-${viewport.name}-${Date.now()}@forgefit.app`;
      const reg = await request.post(`${API_BASE}/api/auth/register`, {
        data: { email, firstName: 'B', lastName: 'N', password: PASSWORD },
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
          activeCalories: 240
        },
        ignoreHTTPSErrors: true
      });
      const sessionId = ((await seed.json()) as CreatedSession).id;

      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const signIn = new SignInPage(page);
      await signIn.goto();
      await page.getByTestId('sign-in-remember-me').click();
      await signIn.signIn(email, PASSWORD);
      await page.waitForURL('**/dashboard');

      for (const route of ['/workouts/new', `/workouts/${sessionId}`, '/profile', '/rewards']) {
        await page.goto(route);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(300);

        // Scroll the inner main container to the bottom so we exercise the
        // case where the nav must stay glued to the viewport edge while
        // content scrolls beneath it.
        await page.evaluate(() => {
          const main = document.querySelector('.app-shell__main');
          if (main) main.scrollTop = main.scrollHeight;
          window.scrollTo(0, document.body.scrollHeight);
        });
        await page.waitForTimeout(150);

        const nav = page.locator('forge-bottom-nav');
        await expect(nav, `route=${route}`).toBeVisible();
        const box = await nav.boundingBox();
        expect(box, `route=${route}`).not.toBeNull();

        const navBottom = box!.y + box!.height;
        // Allow ~2px of fractional rounding.
        expect(
          Math.abs(navBottom - viewport.height),
          `route=${route} navBottom=${navBottom} viewport=${viewport.height}`
        ).toBeLessThan(3);
      }
    });
  }
});
