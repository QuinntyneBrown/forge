// Acceptance Test for Bug 021
// Description: No icon span on /dashboard should leak its ligature text. The
// material-symbols-rounded class needs the matching webfont, or the markup
// needs to use material-icons (which already ships in index.html). This spec
// asserts that every <span class*="material"> element renders as a single
// glyph (the font ligature width is 1 char) — no "play_arrow" or "wb_sunny"
// strings.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard icons render as glyphs', () => {
  test('no material-symbols span leaks its ligature text', async ({ page, request }) => {
    const email = `icons-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'I', lastName: 'C', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await page.waitForURL('**/dashboard');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(800);

    const leaks = await page.evaluate(() => {
      const spans = Array.from(
        document.querySelectorAll<HTMLElement>(
          'main.dashboard span.material-symbols-rounded, main.dashboard span.material-icons'
        )
      );
      return spans
        .map((el) => {
          const fontFamily = getComputedStyle(el).fontFamily.toLowerCase();
          const text = (el.textContent || '').trim();
          // The icon font collapses ligatures — but only if the font is
          // actually applied. If font-family doesn't reference a Material
          // font, the browser falls back to the body font and the literal
          // ligature string ("play_arrow") shows up.
          const usingIconFont =
            fontFamily.includes('material symbols') ||
            fontFamily.includes('material icons');
          return usingIconFont ? null : text;
        })
        .filter((t): t is string => t !== null && t.length > 2);
    });

    expect(
      leaks,
      `expected every dashboard icon span to use a Material icon font; leaks: ${JSON.stringify(leaks)}`
    ).toEqual([]);
  });
});
