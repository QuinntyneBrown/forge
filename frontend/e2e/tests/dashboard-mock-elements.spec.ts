// Acceptance Test for Bug 004
// Description: The /dashboard page should expose the elements enumerated in
// docs/bugs/004-dashboard-does-not-match-mock.md, matching docs/mocks/dashboard.html:
//   - Sticky header with hamburger menu, notification bell, avatar
//   - Greeting block ("Good morning, {firstName}" + date/time line)
//   - "+ Log workout" floating action button (FAB)

import { expect, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test.describe('Dashboard mock alignment', () => {
  test('renders the header chrome, greeting, and FAB from the mock', async ({ page, request }) => {
    const email = `dashmock-${Date.now()}@forgefit.app`;
    const firstName = 'Quinntyne';
    const register = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName, lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(register.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);
    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    // Header chrome
    const menuBtn = page.getByTestId('app-shell-menu-button');
    const notifBtn = page.getByTestId('app-shell-notification-button');
    const avatar = page.getByTestId('app-shell-avatar');
    await expect(menuBtn).toBeVisible();
    await expect(notifBtn).toBeVisible();
    await expect(avatar).toBeVisible();

    // Avatar displays the user's first initial.
    await expect(avatar).toContainText(firstName.charAt(0).toUpperCase());

    // Sticky header: position should resolve to sticky (or fixed) so it stays
    // pinned. Assert the computed CSS position on the topbar.
    const topbarPosition = await page
      .getByTestId('app-shell-topbar')
      .evaluate((el) => getComputedStyle(el).position);
    expect(['sticky', 'fixed']).toContain(topbarPosition);

    // Greeting block
    const greetingHi = page.getByTestId('dashboard-greeting-hi');
    const greetingName = page.getByTestId('dashboard-greeting-name');
    const greetingDate = page.getByTestId('dashboard-greeting-date');
    await expect(greetingHi).toBeVisible();
    await expect(greetingHi).toHaveText(/good (morning|afternoon|evening)/i);
    await expect(greetingName).toContainText(firstName);
    await expect(greetingDate).toBeVisible();

    // Log workout FAB
    const fab = page.getByTestId('dashboard-log-workout-fab');
    await expect(fab).toBeVisible();
    await expect(fab).toContainText(/log workout/i);
  });
});
