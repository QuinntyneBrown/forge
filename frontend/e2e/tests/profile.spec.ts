// Acceptance Test
// Traces to: L2-005 (profile management — fields covered by BT-015)
// Description: A signed-in user navigates to /profile, sees their current
// values pre-filled from GET /api/me, edits the seven BT-015 fields, saves,
// and (after a fresh GET) sees the updated values persisted. The window /
// nudge / leaderboard toggles aren't yet exposed by the backend (BT-018 /
// BT-019 / BT-020) so the form omits them.

import { expect, test } from '@playwright/test';
import { ProfilePage } from '../pages/profile.page';
import { SignInPage } from '../pages/sign-in.page';

const TEST_PASSWORD = 'ForgeFit!2026';
const API_BASE = 'https://localhost:5001';

test('profile form persists the BT-015 fields and reflects them on a re-fetch', async ({ page, request }) => {
  const email = `profile-${Date.now()}@forgefit.app`;
  const reg = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email, firstName: 'Quinn', lastName: 'Forge', password: TEST_PASSWORD },
    ignoreHTTPSErrors: true
  });
  expect(reg.ok()).toBeTruthy();

  const signIn = new SignInPage(page);
  await signIn.goto();
  // Remember me so a full-page navigation to /profile rehydrates the session
  // via provideAppInitializer's tryHydrate() instead of the auth guard
  // bouncing the test back to /sign-in.
  await page.getByTestId('sign-in-remember-me').click();
  await signIn.signIn(email, TEST_PASSWORD);
  await page.waitForURL('**/dashboard');

  const profile = new ProfilePage(page);
  await profile.goto();

  // Defaults from BT-015 migration prefill the form.
  await expect(profile.firstName).toHaveValue('Quinn');
  await expect(profile.lastName).toHaveValue('Forge');
  await expect(profile.email).toHaveValue(email);
  await expect(profile.caloriesTarget).toHaveValue('1500');
  await expect(profile.minutesTarget).toHaveValue('60');

  const newEmail = `profile-renamed-${Date.now()}@forgefit.app`;
  await profile.firstName.fill('Quinntyne');
  await profile.lastName.fill('Brown');
  await profile.email.fill(newEmail);
  await profile.timeZone.fill('America/Toronto');
  await profile.caloriesTarget.fill('1800');
  await profile.minutesTarget.fill('75');

  const saveResponse = page.waitForResponse((r) => r.url().endsWith('/api/profile'));
  await profile.saveButton.click();
  const response = await saveResponse;
  expect(response.status()).toBe(204);

  await expect(profile.savedBanner).toBeVisible();

  // Reload the page to confirm persistence — the form re-initializes from
  // GET /api/me, which now should return the saved values.
  await page.reload();
  await expect(profile.firstName).toHaveValue('Quinntyne');
  await expect(profile.lastName).toHaveValue('Brown');
  await expect(profile.email).toHaveValue(newEmail);
  await expect(profile.timeZone).toHaveValue('America/Toronto');
  await expect(profile.caloriesTarget).toHaveValue('1800');
  await expect(profile.minutesTarget).toHaveValue('75');
});
