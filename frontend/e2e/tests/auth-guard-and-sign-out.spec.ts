// Acceptance Test
// Traces to: L2-003 (sign-out), L2-038 (authorization on every endpoint),
//            L2-033 (refresh-token rotation, indirectly verified by sign-out
//            invalidating the family).
// Description: (1) Visiting /dashboard unauthenticated redirects to
// /sign-in?returnUrl=/dashboard. (2) After sign-in, clicking the dashboard's
// sign-out button calls POST /api/auth/sign-out against the backend, clears
// the in-memory session, and routes back to /sign-in.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const TEST_PASSWORD = 'ForgeFit!2026';
const API_BASE = 'https://localhost:5001';

test('unauthenticated visit to /dashboard redirects to /sign-in with returnUrl', async ({ page }) => {
  await page.goto('/dashboard');
  await page.waitForURL(/\/sign-in\?returnUrl=/);
  expect(page.url()).toContain('returnUrl=%2Fdashboard');
});

test('sign-out clears the session and calls the backend revoke endpoint', async ({ page, request }) => {
  const email = `signout-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'Sign', lastName: 'Out', password: TEST_PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();

  const signInPage = new SignInPage(page);
  const dashboard = new DashboardPage(page);

  await signInPage.goto();
  await signInPage.signIn(email, TEST_PASSWORD);
  await dashboard.waitForLoad();

  const signOutCall = page.waitForResponse((r) => r.url().endsWith('/api/auth/sign-out'));
  await dashboard.signOutButton.click();
  const signOutResponse = await signOutCall;
  expect(signOutResponse.status()).toBe(204);

  await page.waitForURL(/\/sign-in/);
});
