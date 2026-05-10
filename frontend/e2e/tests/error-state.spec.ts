// Acceptance Test
// Traces to: FT-033, L2-024, L2-029
// Description: The /error route surfaces the supplied traceId in the
// footer meta and exposes a "Go to dashboard" CTA. Clicking the CTA
// triggers a navigation; for an anonymous visitor that resolves to
// /sign-in via the auth guard.

import { expect, test } from '@playwright/test';

test('error page surfaces traceId and provides a dashboard CTA', async ({ page }) => {
  await page.goto('/error?traceId=abc123def456');

  await page.getByTestId('error-page-banner').waitFor();

  await expect(page.getByTestId('error-page-footer-meta')).toContainText('abc123def456');
  await expect(page.getByTestId('error-page-banner')).toContainText('Apple Watch sync paused');
  await expect(page.getByTestId('error-page-diagnostics')).toBeVisible();

  await page.getByTestId('error-page-dashboard-button').click();
  // Anonymous user trying to reach /dashboard gets redirected to /sign-in
  // by authGuard with returnUrl=/dashboard. We accept either landing page;
  // the navigation away from /error is what proves the CTA fired.
  await page.waitForURL((url) => !url.pathname.startsWith('/error'), {
    timeout: 5000
  });
  expect(page.url()).toMatch(/\/(sign-in|dashboard)/);
});
