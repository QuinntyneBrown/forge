// Acceptance Test
// Traces to: L2-004 (confirm leg)
// Description: Visiting /password-reset?token=xxx renders the confirm form.
// Submitting a new password posts to /api/auth/password-reset/confirm.
// On 204 the UI navigates to /sign-in. On 400 (invalid / expired / reused
// token) the UI shows the error message and stays on the page.

import { expect, test } from '@playwright/test';

test('confirm leg navigates to /sign-in after a 204 response', async ({ page }) => {
  // The raw token is only emitted to the deferred email log, so we can't
  // recover one from the issuing flow without breaking the no-enumeration
  // contract. Mock the confirm endpoint to drive the happy-path UI.
  await page.route('**/api/auth/password-reset/confirm', async (route) => {
    expect(route.request().method()).toBe('POST');
    const body = route.request().postDataJSON();
    expect(body).toMatchObject({ token: 'fake-token-xyz' });
    expect(typeof body.newPassword).toBe('string');
    await route.fulfill({ status: 204, body: '' });
  });

  await page.goto('/password-reset?token=fake-token-xyz');

  await page.getByTestId('password-reset-confirm-new-password').fill('ZebraQuokka!9z!2026');
  await page.getByTestId('password-reset-confirm-submit').click();

  await page.waitForURL(/\/sign-in/);
});

test('confirm leg shows the error message on a 400 from the backend', async ({ page }) => {
  // Hit the live backend with a bogus token; the backend returns 400 because
  // no PasswordResetTokens row matches that hash. The form's error handler
  // surfaces the title from the ProblemDetails body.
  await page.goto('/password-reset?token=bogus-token-that-does-not-exist');

  await page.getByTestId('password-reset-confirm-new-password').fill('ZebraQuokka!9z!2026');
  await page.getByTestId('password-reset-confirm-submit').click();

  await expect(page.getByTestId('password-reset-confirm-error')).toBeVisible();
});
