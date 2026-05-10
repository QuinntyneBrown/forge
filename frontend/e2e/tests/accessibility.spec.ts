// Acceptance Test
// Traces to: FT-035, L2-045, L2-047, L2-048
// Description: Runs axe-core's WCAG 2.0 A + AA rule pack against every
// route. Authenticated routes get a fresh signed-in session; the
// unauthenticated routes (sign-in, sign-up, password-reset, error,
// /does-not-exist) are visited directly. Any violation fails the build.

import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

const ANONYMOUS_ROUTES = [
  '/sign-in',
  '/sign-up',
  '/password-reset',
  '/error?traceId=test',
  '/does-not-exist'
];

const AUTHENTICATED_ROUTES = [
  '/dashboard',
  '/workouts',
  '/workouts/new',
  '/rewards',
  '/profile'
];

async function scan(page: import('@playwright/test').Page, route: string): Promise<void> {
  await page.goto(route);
  // Give cards / forms a tick to render before scanning.
  await page.waitForLoadState('networkidle').catch(() => undefined);

  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa'])
    .analyze();

  if (results.violations.length > 0) {
    const summary = results.violations
      .map((v) => `${v.id} (${v.impact}): ${v.help} — ${v.nodes.length} node(s)`)
      .join('\n');
    expect(results.violations, `axe found violations on ${route}:\n${summary}`).toEqual([]);
  }
}

test.describe('axe-core WCAG 2.0 A + AA', () => {
  for (const route of ANONYMOUS_ROUTES) {
    test(`anonymous: ${route}`, async ({ page }) => {
      await scan(page, route);
    });
  }

  test.describe('authenticated', () => {
    test.beforeEach(async ({ page, request }) => {
      const email = `a11y-${Date.now()}-${Math.random().toString(36).slice(2, 7)}@forgefit.app`;
      const register = await request.post(`${API_BASE}/api/auth/register`, {
        data: { email, firstName: 'A11y', lastName: 'Test', password: PASSWORD },
        ignoreHTTPSErrors: true
      });
      expect(register.ok()).toBeTruthy();

      const signIn = new SignInPage(page);
      await signIn.goto();
      await signIn.signIn(email, PASSWORD);
      await page.waitForURL('**/dashboard');
    });

    for (const route of AUTHENTICATED_ROUTES) {
      test(`authed: ${route}`, async ({ page }) => {
        await scan(page, route);
      });
    }
  });
});
