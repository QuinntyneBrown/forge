// Acceptance Test for Bug 009
// Description: The Material 3 primary color token across the app must render
// as the Forge Fit teal #106B5C declared in the design mocks
// (see docs/mocks/dashboard.html: --md-sys-color-primary:#106B5C). The
// implementation currently wires Material's azure palette and renders blue
// everywhere — buttons, the active rail item, the calorie ring, etc.
//
// This spec covers two surfaces — one auth page (no login) and one logged-in
// page — to prove the token cascade is correct end to end.

import { expect, Page, test } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const API_BASE = 'https://localhost:5001';
const PASSWORD = 'ForgeFit!2026';

// Canonical teal pulled directly from docs/mocks/dashboard.html:
//   --md-sys-color-primary:#106B5C
const TEAL_HEX = '#106b5c';
const TEAL_RGB = { r: 0x10, g: 0x6b, b: 0x5c };

interface Rgb {
  r: number;
  g: number;
  b: number;
}

function parseRgb(value: string): Rgb {
  const match = value.match(/(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/);
  if (!match) {
    throw new Error(`Could not parse rgb-ish value: ${value}`);
  }
  return { r: Number(match[1]), g: Number(match[2]), b: Number(match[3]) };
}

function parseHex(value: string): Rgb {
  const cleaned = value.trim().replace(/^#/, '');
  return {
    r: parseInt(cleaned.slice(0, 2), 16),
    g: parseInt(cleaned.slice(2, 4), 16),
    b: parseInt(cleaned.slice(4, 6), 16)
  };
}

function parseColor(value: string): Rgb {
  const trimmed = value.trim();
  // The M3 system tokens are emitted as `light-dark(#xxxxxx, #yyyyyy)`. The
  // app forces `color-scheme: light` so the first argument is what users see.
  const lightDark = trimmed.match(/^light-dark\(\s*([^,]+)\s*,/);
  if (lightDark) return parseColor(lightDark[1]);
  if (trimmed.startsWith('#')) return parseHex(trimmed);
  if (trimmed.startsWith('rgb')) return parseRgb(trimmed);
  // Allow bare hex without leading "#"
  if (/^[0-9a-fA-F]{6}$/.test(trimmed)) return parseHex(trimmed);
  throw new Error(`Unrecognised color format: ${value}`);
}

function colorDistance(a: Rgb, b: Rgb): number {
  return Math.sqrt(
    (a.r - b.r) ** 2 + (a.g - b.g) ** 2 + (a.b - b.b) ** 2
  );
}

/**
 * The mock token is `#106B5C`. We allow a small distance (perceptual wiggle
 * room) but reject anything that lands in the blue family. Material's azure
 * primary at default density resolves to roughly rgb(64, 95, 144) which is a
 * distance of ~95 from teal — far outside this tolerance.
 */
function expectCloseToTeal(actual: string, label: string): void {
  const rgb = parseColor(actual);
  const distance = colorDistance(rgb, TEAL_RGB);
  const message = `${label}: expected ~${TEAL_HEX}, got ${actual} (rgb ${rgb.r},${rgb.g},${rgb.b}; distance=${distance.toFixed(1)})`;
  expect(distance, message).toBeLessThan(35);
}

async function readPrimaryToken(page: Page): Promise<string> {
  return page.evaluate(() => {
    const root = document.documentElement;
    const value = getComputedStyle(root).getPropertyValue('--mat-sys-primary');
    return value.trim();
  });
}

test.describe('Bug 009: Primary color token resolves to mock teal', () => {
  test('Sign-in submit button background renders in teal', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();

    // Fill the form so the submit button becomes enabled — disabled
    // mat-flat-buttons paint a different (muted) background.
    await signIn.emailInput.fill('any@example.com');
    await signIn.passwordInput.fill('any-password-value');
    await expect(signIn.submitButton).toBeEnabled();

    // The mat-sys-primary CSS custom property is the canonical M3 token; if
    // this is wrong, every primary surface in the app is wrong.
    const primaryToken = await readPrimaryToken(page);
    expectCloseToTeal(primaryToken, '--mat-sys-primary on /sign-in');

    // The rendered submit button background must match the token.
    const bg = await signIn.submitButton.evaluate(
      (el) => getComputedStyle(el).backgroundColor
    );
    expectCloseToTeal(bg, 'sign-in submit background');
  });

  test('Dashboard primary token renders in teal for a signed-in user', async ({
    page,
    request
  }) => {
    const email = `bug009-${Date.now()}@forgefit.app`;
    const register = await request.post(`${API_BASE}/api/auth/register`, {
      data: { email, firstName: 'Teal', lastName: 'Test', password: PASSWORD },
      ignoreHTTPSErrors: true
    });
    expect(register.ok()).toBeTruthy();

    const signIn = new SignInPage(page);
    const dashboard = new DashboardPage(page);

    await signIn.goto();
    await signIn.signIn(email, PASSWORD);
    await dashboard.waitForLoad();

    const primaryToken = await readPrimaryToken(page);
    expectCloseToTeal(primaryToken, '--mat-sys-primary on /dashboard');

    // The dashboard "Sign out" outline button is bound to the primary token
    // for both its label colour and its border colour
    // (see dashboard.page.scss: color: var(--md-sys-color-primary, ...)).
    // Anchor at least one rendered element to the token so a regression in
    // the cascade (custom CSS overriding the token, etc.) is also caught.
    await expect(dashboard.signOutButton).toBeVisible();
    const signOutColor = await dashboard.signOutButton.evaluate(
      (el) => getComputedStyle(el).color
    );
    expectCloseToTeal(signOutColor, 'dashboard sign-out label color');
  });
});
