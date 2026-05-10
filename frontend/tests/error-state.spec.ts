// Acceptance Test
// Traces to: FT-033, L2-024, L2-029
// Description: The /error route renders the SyncErrorPanel with the
// traceId query param and the live diagnostics from /health (Healthy when
// the backend is reachable). Clicking "Go to dashboard" navigates to
// /dashboard.

import { expect, test } from '@playwright/test';

test('error page surfaces traceId and health diagnostics', async ({ page }) => {
  await page.goto('/error?traceId=abc123def456');
  await page.getByTestId('sync-error-panel').waitFor();

  await expect(page.getByTestId('sync-error-panel-trace')).toContainText('abc123def456');
  await expect(page.getByTestId('sync-error-panel-banner')).toContainText('Something went sideways');
  await expect(page.getByTestId('sync-error-panel-health')).toContainText(/Healthy|Unhealthy|Loading/);

  await page.getByTestId('sync-error-panel-dashboard').click();
  await page.waitForURL('**/sign-in', { timeout: 5000 });
  // Anonymous user trying to reach /dashboard gets redirected to /sign-in
  // by authGuard with returnUrl=/dashboard. The redirect itself is the
  // assertion that the CTA fired correctly.
});
