// Acceptance Test
// Traces to: L2-001 (registration)
// Description: A new user fills the sign-up form, the backend creates the
// account, and the app auto-signs the user in and lands on the dashboard.
// Exercises FT-012 (SignUpFormComponent) end to end against the live backend.

import { expect, test } from '@playwright/test';
import { DashboardPage } from './pom/dashboard.page';
import { SignUpPage } from './pom/sign-up.page';

test('signs up a new account and lands on the dashboard', async ({ page }) => {
  const email = `signup-${Date.now()}@forgefit.app`;
  const password = 'ForgeFit!2026';

  const signUp = new SignUpPage(page);
  const dashboard = new DashboardPage(page);

  await signUp.goto();
  await signUp.signUp('Sign', 'Up', email, password);

  await dashboard.waitForLoad();

  await expect(dashboard.greeting).toContainText(email);
  await expect(dashboard.greeting).toContainText('User');
});
