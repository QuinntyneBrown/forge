// Screenshot Capture Spec
// Captures full-page screenshots of every implemented Forge frontend screen at
// three form factors (desktop / tablet / mobile) for diffing against the design
// mocks under docs/mocks/.
//
// NOT part of the default e2e run. Invoke explicitly:
//   npx playwright test e2e/screenshots/capture-screens.spec.ts --project=chromium
//
// Requires the backend to be running (so the seeded dev user can sign in) and
// the frontend (Playwright's webServer block boots it automatically).

import { Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';

interface FormFactor {
  name: 'desktop' | 'tablet' | 'mobile';
  width: number;
  height: number;
}

const FORM_FACTORS: FormFactor[] = [
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'tablet', width: 834, height: 1112 },
  { name: 'mobile', width: 390, height: 844 }
];

// Resolve once relative to this spec file. From frontend/e2e/screenshots/ this
// climbs two levels to the repo root, then into docs/screenshots.
const SCREENSHOT_ROOT = resolve(__dirname, '..', '..', '..', 'docs', 'screenshots');

for (const ff of FORM_FACTORS) {
  mkdirSync(resolve(SCREENSHOT_ROOT, ff.name), { recursive: true });
}

async function settle(page: Page): Promise<void> {
  await page.waitForLoadState('networkidle').catch(() => {
    /* networkidle can hang on Angular polling; the timeout below covers it */
  });
  await page.waitForTimeout(400);
}

async function snap(page: Page, ff: FormFactor, name: string): Promise<void> {
  const path = resolve(SCREENSHOT_ROOT, ff.name, `${name}.png`);
  await page.screenshot({ path, fullPage: true });
}

async function signIn(page: Page): Promise<void> {
  const signInPage = new SignInPage(page);
  await signInPage.goto();
  await signInPage.signIn(DEV_EMAIL, DEV_PASSWORD);
  await page.waitForURL('**/dashboard', { timeout: 30_000 });
  await page.getByTestId('dashboard-greeting').waitFor();
}

for (const ff of FORM_FACTORS) {
  test.describe(`screenshots @ ${ff.name} (${ff.width}x${ff.height})`, () => {
    test.use({ viewport: { width: ff.width, height: ff.height } });

    test(`capture all screens at ${ff.name}`, async ({ page }) => {
      test.setTimeout(180_000);

      // ----- Public (no auth) screens -----
      await page.goto('/sign-in');
      await page.getByTestId('sign-in-email').waitFor();
      await settle(page);
      await snap(page, ff, 'sign-in');

      await page.goto('/sign-up');
      await page.getByTestId('sign-up-email').waitFor().catch(() => {});
      await settle(page);
      await snap(page, ff, 'sign-up');

      await page.goto('/password-reset');
      await settle(page);
      await snap(page, ff, 'password-reset');

      await page.goto('/error');
      await settle(page);
      await snap(page, ff, 'error-state');

      await page.goto('/this-route-does-not-exist');
      await settle(page);
      await snap(page, ff, 'not-found');

      // ----- Sign in as the seeded dev user -----
      await signIn(page);
      await settle(page);
      await snap(page, ff, 'dashboard');

      // ----- Auth-protected screens -----
      await page.goto('/profile');
      await settle(page);
      await snap(page, ff, 'profile');

      await page.goto('/workouts');
      await page.getByTestId('workout-list').waitFor();
      await settle(page);
      await snap(page, ff, 'workouts');

      // Workout detail: pick the first row from the list if one exists.
      const firstRow = page.getByTestId('workout-list-rows').locator('li').first();
      const hasRow = (await firstRow.count()) > 0;
      if (hasRow) {
        await firstRow.click();
        await page.waitForURL(/\/workouts\/[^/]+$/, { timeout: 15_000 });
        await settle(page);
        await snap(page, ff, 'workout-detail');
      } else {
        // No seeded sessions — record the empty list as workout-detail-missing
        // so the diff step has a leads-trail.
        await snap(page, ff, 'workout-detail-missing');
      }

      await page.goto('/workouts/new');
      await settle(page);
      await snap(page, ff, 'workouts-new');

      await page.goto('/rewards');
      await settle(page);
      await snap(page, ff, 'rewards');
    });
  });
}
