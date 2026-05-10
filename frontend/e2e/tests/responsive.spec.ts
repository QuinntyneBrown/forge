// Acceptance Test
// Traces to: L2-010 (equipment surfaces), L2-013 (dashboard summary),
//            L2-030 (responsive XS/SM/MD/LG/XL), L2-046 (touch targets).
// Description: At < 992px the dashboard renders with the bottom navigation
// strip; at >= 992px it renders with the desktop nav rail. The shell brand /
// app bar is always visible. Smoke-tested on the dashboard which uses
// <forge-app-shell>.

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const TEST_PASSWORD = 'ForgeFit!2026';
const API_BASE = 'https://localhost:5001';

async function signedInDashboard(browser: import('@playwright/test').Browser, viewport: { width: number; height: number }) {
  const email = `responsive-${Date.now()}-${viewport.width}@forgefit.app`;
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true, viewport });
  const reg = await ctx.request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'R', lastName: 'V', password: TEST_PASSWORD }
  });
  expect(reg.ok()).toBeTruthy();
  const page = await ctx.newPage();
  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);
  await signIn.goto();
  await signIn.signIn(email, TEST_PASSWORD);
  await dashboard.waitForLoad();
  return { ctx, page };
}

test('shows the bottom navigation strip at 360px (mobile)', async ({ browser }) => {
  const { ctx, page } = await signedInDashboard(browser, { width: 360, height: 800 });
  await expect(page.getByTestId('app-shell-bottom-nav')).toBeVisible();
  await expect(page.getByTestId('app-shell-nav-rail')).toBeHidden();
  await ctx.close();
});

test('shows the desktop navigation rail at 1440px', async ({ browser }) => {
  const { ctx, page } = await signedInDashboard(browser, { width: 1440, height: 900 });
  await expect(page.getByTestId('app-shell-nav-rail')).toBeVisible();
  await expect(page.getByTestId('app-shell-bottom-nav')).toBeHidden();
  await ctx.close();
});
