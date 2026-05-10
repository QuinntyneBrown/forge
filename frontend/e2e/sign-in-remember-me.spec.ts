// Acceptance Test
// Traces to: L2-002 (sign-in, ac 2 — Remember me survives browser restart),
//            L2-033 (refresh-token rotation on hydration).
// Description: Sign in with Remember me checked, capture the browser context's
// storageState, and open a fresh context that inherits only the persisted
// localStorage. Navigating to /dashboard in the fresh context succeeds without
// re-prompting for credentials because AuthStateService rehydrates the session
// from the persisted refresh token. Sign in without Remember me and the same
// fresh-context flow lands on /sign-in.

import { expect, test } from '@playwright/test';
import { DashboardPage } from './pom/dashboard.page';
import { SignInPage } from './pom/sign-in.page';

const TEST_PASSWORD = 'ForgeFit!2026';
const API_BASE = 'https://localhost:5001';

test.describe('Remember me', () => {
  test('persists the session across a browser restart when checked', async ({ browser, request }) => {
    const email = `remember-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'R', lastName: 'M', password: TEST_PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    // First context: sign in with Remember me checked.
    const ctx1 = await browser.newContext({ ignoreHTTPSErrors: true });
    const page1 = await ctx1.newPage();
    const signIn = new SignInPage(page1);
    const dashboard = new DashboardPage(page1);
    await signIn.goto();
    await page1.getByTestId('sign-in-remember-me').click();
    await signIn.signIn(email, TEST_PASSWORD);
    await dashboard.waitForLoad();

    const storageState = await ctx1.storageState();
    await ctx1.close();

    // Second context: only inherits the persisted localStorage. Navigating to
    // /dashboard rehydrates from the refresh token without re-prompting.
    const ctx2 = await browser.newContext({ ignoreHTTPSErrors: true, storageState });
    const page2 = await ctx2.newPage();
    const dashboard2 = new DashboardPage(page2);
    await page2.goto('/dashboard');
    await dashboard2.waitForLoad();
    await expect(dashboard2.greeting).toContainText(email);
    await ctx2.close();
  });

  test('does not persist the session when Remember me is unchecked', async ({ browser, request }) => {
    const email = `noremember-${Date.now()}@forgefit.app`;
    const reg = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'N', lastName: 'R', password: TEST_PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(reg.ok()).toBeTruthy();

    const ctx1 = await browser.newContext({ ignoreHTTPSErrors: true });
    const page1 = await ctx1.newPage();
    const signIn = new SignInPage(page1);
    const dashboard = new DashboardPage(page1);
    await signIn.goto();
    // Remember me intentionally NOT checked.
    await signIn.signIn(email, TEST_PASSWORD);
    await dashboard.waitForLoad();

    const storageState = await ctx1.storageState();
    await ctx1.close();

    const ctx2 = await browser.newContext({ ignoreHTTPSErrors: true, storageState });
    const page2 = await ctx2.newPage();
    await page2.goto('/dashboard');
    await page2.waitForURL(/\/sign-in/);
    await ctx2.close();
  });
});
