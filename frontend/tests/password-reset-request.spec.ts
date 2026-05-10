// Acceptance Test
// Traces to: L2-004 (request leg)
// Description: A user navigates to /password-reset, enters an email, submits.
// The frontend posts to /api/auth/password-reset/request, receives 202, and
// shows a confirmation message that does not reveal whether the email exists.
// Tests both an unknown email (no account enumeration) and a known one — both
// land on the same confirmation copy.

import { expect, test } from '@playwright/test';

const API_BASE = 'https://localhost:5001';

test('password-reset request submits and shows the same confirmation regardless of account existence', async ({ page, request }) => {
  // Pre-register one account so we can drive both branches against the live
  // backend. Each branch uses its own email so the per-test DB isn't shared.
  const knownEmail = `pwr-known-${Date.now()}@forgefit.app`;
  const reg = await request.post(`${API_BASE}/api/auth/register`, {
    data: { email: knownEmail, firstName: 'P', lastName: 'R', password: 'ForgeFit!2026' },
    ignoreHTTPSErrors: true
  });
  expect(reg.ok()).toBeTruthy();

  for (const email of [knownEmail, `pwr-unknown-${Date.now()}@forgefit.app`]) {
    await page.goto('/password-reset');

    const submitResponse = page.waitForResponse(
      (r) => r.url().endsWith('/api/auth/password-reset/request')
    );

    await page.getByTestId('password-reset-request-email').fill(email);
    await page.getByTestId('password-reset-request-submit').click();

    const response = await submitResponse;
    expect(response.status()).toBe(202);

    await expect(page.getByTestId('password-reset-request-confirmation')).toBeVisible();
  }
});
