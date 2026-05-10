// Acceptance Test for Bug 009
// Description: All filled primary buttons should render in the Forge Fit
// teal #106B5C — not Material's default azure blue. The Sign-in submit and
// the workout-new save are good representative samples.

import { expect, test } from '@playwright/test';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

function rgbToHex(rgb: string): string {
  const m = rgb.match(/(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/);
  if (!m) return rgb;
  return (
    '#' +
    [m[1], m[2], m[3]]
      .map((n) => Number(n).toString(16).padStart(2, '0'))
      .join('')
      .toLowerCase()
  );
}

test.describe('Primary color theming', () => {
  test('Sign in submit button uses the Forge Fit teal, not blue', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();
    await page.getByTestId('sign-in-email').fill('any@example.com');
    await page.getByTestId('sign-in-password').fill('any-password');

    // Inspect the actual rendered background of the submit button (now
    // enabled because the form is valid). The button is wrapped by
    // <forge-button> which renders a Material mat-flat-button.
    const submit = page.getByTestId('sign-in-submit');
    await expect(submit).toBeEnabled();
    const bg = await submit.evaluate((el) => getComputedStyle(el).backgroundColor);
    const hex = rgbToHex(bg);
    // Reject the Material azure blue family (anything starting with #00..#3f
    // in the red channel and >0x80 in blue is a blue button).
    const m = bg.match(/(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/)!;
    const [r, g, b] = [Number(m[1]), Number(m[2]), Number(m[3])];
    // Teal/green has g > r and g > b (or close); azure blue has b > g.
    expect(g, `expected teal-ish, got ${hex} (${bg})`).toBeGreaterThan(r);
    expect(g, `expected teal-ish, got ${hex} (${bg})`).toBeGreaterThanOrEqual(b - 20);
  });
});
