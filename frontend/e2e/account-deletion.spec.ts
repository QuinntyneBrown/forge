// Acceptance Test
// Traces to: FT-032, L2-006, L2-050
// Description: A signed-in user deletes their account from the profile
// page. The Delete-account CTA requires a second confirm click (no
// double-click can mistakenly destroy data). After confirmation, the
// session is cleared and the user is routed to /sign-in. Trying to
// sign in again with the same credentials returns 401 because BT-006
// soft-deleted the user.

import { expect, test } from '@playwright/test';
import { SignInPage } from './pom/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

test('deleting an account signs the user out and blocks re-signin', async ({ page, request }) => {
  const email = `del-${Date.now()}@forgefit.app`;
  const register = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'Del', lastName: 'Test', password: PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(register.ok()).toBeTruthy();

  const signIn = new SignInPage(page);
  await signIn.goto();
  await signIn.signIn(email, PASSWORD);
  await page.waitForURL('**/dashboard');

  await page.goto('/profile');
  await page.getByTestId('profile-danger-zone').waitFor();

  // First click reveals the confirm prompt — no destructive action yet.
  await page.getByTestId('profile-delete-account').click();
  await expect(page.getByTestId('profile-delete-confirm-prompt')).toBeVisible();

  // Second click finalises the deletion and routes to /sign-in.
  await page.getByTestId('profile-delete-confirm').click();
  await page.waitForURL('**/sign-in', { timeout: 10000 });

  // The same email/password should now fail to sign in.
  await signIn.signIn(email, PASSWORD);
  await expect(page.getByTestId('sign-in-error')).toBeVisible();
});
