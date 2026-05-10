// Acceptance Test for Bug 013
// Description: /error must show a top banner, hero illustration, error code
// chip, two CTAs (Retry sync + Go to dashboard), a 4-row Diagnostics card,
// and a troubleshooting/contact footer — per docs/mocks/error-state.html.

import { expect, test } from '@playwright/test';

test.describe('Error state mock alignment', () => {
  test('renders banner, hero, code chip, CTAs, diagnostics, footer', async ({ page }) => {
    await page.goto('/error');
    await page.waitForLoadState('networkidle');

    await expect(page.getByTestId('error-page-banner')).toBeVisible();
    await expect(page.getByTestId('error-page-hero')).toBeVisible();
    await expect(page.getByTestId('error-page-code-chip')).toBeVisible();
    await expect(page.getByTestId('error-page-retry-button')).toBeVisible();
    await expect(page.getByTestId('error-page-dashboard-button')).toBeVisible();

    const diagnostics = page.getByTestId('error-page-diagnostics');
    await expect(diagnostics).toBeVisible();
    const diagnosticRows = page.locator('[data-testid="error-page-diagnostic-row"]');
    expect(await diagnosticRows.count()).toBeGreaterThanOrEqual(4);

    await expect(page.getByTestId('error-page-troubleshooting-link')).toBeVisible();
    await expect(page.getByTestId('error-page-footer-meta')).toBeVisible();
  });
});
