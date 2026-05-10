// Acceptance Test for Bug 020
// Description: Locks in the auth-page styling + content the audit flagged:
//   - Mobile (390x844): curved-hero corners + card-overlap layout, pill-shaped
//     primary CTA, sign-up perks list, password strength meter, Terms-of-Service
//     consent (submit disabled until checked), password-reset success state.
//   - Tablet (834x1112): the auth wrapper renders as a 2-column grid.
// Each block scopes its own viewport via test.use({ viewport }) so we only
// assert what the bug calls out at that breakpoint.

import { Locator, expect, test } from '@playwright/test';
import { PasswordResetPage } from '../pages/password-reset.page';
import { SignInPage } from '../pages/sign-in.page';
import { SignUpPage } from '../pages/sign-up.page';

const MOBILE = { width: 390, height: 844 };
const TABLET = { width: 834, height: 1112 };

async function radius(loc: Locator, side: 'left' | 'right'): Promise<number> {
  const prop = side === 'left' ? 'borderBottomLeftRadius' : 'borderBottomRightRadius';
  const value = await loc.evaluate(
    (el, p) => getComputedStyle(el)[p as 'borderBottomLeftRadius'],
    prop
  );
  return parseFloat(value);
}

async function marginTop(loc: Locator): Promise<number> {
  const value = await loc.evaluate((el) => getComputedStyle(el).marginTop);
  return parseFloat(value);
}

async function borderRadius(loc: Locator): Promise<number> {
  const value = await loc.evaluate((el) => {
    // mat-flat-button renders the actual button tag inside <forge-button>; if
    // we get the host shell we walk down to the deepest button-like child.
    const target = (el.matches('button') ? el : el.querySelector('button')) as HTMLElement | null;
    if (!target) return getComputedStyle(el).borderRadius;
    return getComputedStyle(target).borderRadius;
  });
  // border-radius can come back as e.g. "999px" or as four-corner shorthand.
  return parseFloat(String(value));
}

test.describe('Auth pages — mobile curved hero + card overlap + pill CTA', () => {
  test.use({ viewport: MOBILE });

  for (const route of ['/sign-in', '/sign-up', '/password-reset']) {
    test(`hero on ${route} has curved bottom corners and the form card overlaps it`, async ({
      page
    }) => {
      await page.goto(route);
      await page.waitForLoadState('networkidle');

      // Locate hero + card by their page-level wrapper class so the assertion is
      // route-agnostic. Each auth page uses `.<page>__hero` / `.<page>__card`.
      const hero = page.locator('[class$="-page__hero"]').first();
      const card = page.locator('[class$="-page__card"]').first();

      await expect(hero).toBeVisible();
      await expect(card).toBeVisible();

      // Mock spec: --shape-xl (28px) curved bottom corners.
      expect(await radius(hero, 'left')).toBeGreaterThanOrEqual(20);
      expect(await radius(hero, 'right')).toBeGreaterThanOrEqual(20);

      // Mock spec: card uses negative margin-top to overlap the hero curve.
      expect(await marginTop(card)).toBeLessThan(0);
    });
  }

  test('sign-in primary submit button is pill-shaped', async ({ page }) => {
    const signIn = new SignInPage(page);
    await signIn.goto();
    await page.waitForLoadState('networkidle');

    await expect(signIn.submitButton).toBeVisible();
    // 24+ px is enough to confirm the pill radius rather than the default
    // 4-8px Material rectangle. Mock target is 999px ("--shape-full").
    expect(await borderRadius(signIn.submitButton)).toBeGreaterThanOrEqual(24);
  });
});

test.describe('Sign-up — perks list, strength meter, ToS consent', () => {
  test.use({ viewport: MOBILE });

  test('hero renders the perks list with the mock copy', async ({ page }) => {
    const signUp = new SignUpPage(page);
    await signUp.goto();
    await page.waitForLoadState('networkidle');

    await expect(signUp.perksList).toBeVisible();
    expect(await signUp.perks.count()).toBeGreaterThanOrEqual(3);
    await expect(signUp.perksList).toContainText('-20 lb / month');
    await expect(signUp.perksList).toContainText('1,500 active calories');
    await expect(signUp.perksList).toContainText('Earn points');
  });

  test('password strength meter appears after typing a password', async ({ page }) => {
    const signUp = new SignUpPage(page);
    await signUp.goto();
    await page.waitForLoadState('networkidle');

    // Empty state: meter is hidden (only renders once the user types).
    await expect(signUp.passwordStrengthMeter).toHaveCount(0);

    await signUp.passwordInput.fill('abc');
    await expect(signUp.passwordStrengthMeter).toBeVisible();

    // A stronger password should change the displayed label so the meter is
    // doing real classification work, not just rendering a static element.
    const weakLabel = await signUp.passwordStrengthMeter.innerText();
    await signUp.passwordInput.fill('Sup3rStr0ng!Pass');
    await expect(signUp.passwordStrengthMeter).not.toHaveText(weakLabel);
  });

  test('ToS checkbox gates submit', async ({ page }) => {
    const signUp = new SignUpPage(page);
    await signUp.goto();
    await page.waitForLoadState('networkidle');

    await signUp.firstNameInput.fill('Tos');
    await signUp.lastNameInput.fill('Gate');
    await signUp.emailInput.fill(`tos-${Date.now()}@forgefit.app`);
    await signUp.passwordInput.fill('Sup3rStr0ng!Pass');
    // Bug 028 added a required Confirm password field — fill it to isolate this
    // test on the ToS gating behaviour.
    await signUp.confirmPasswordInput.fill('Sup3rStr0ng!Pass');

    // ToS unchecked => submit disabled.
    await expect(signUp.tosCheckbox).not.toBeChecked();
    await expect(signUp.submitButton).toBeDisabled();

    // Checking the box enables submit.
    await signUp.tosCheckbox.check();
    await expect(signUp.tosCheckbox).toBeChecked();
    await expect(signUp.submitButton).toBeEnabled();
  });
});

test.describe('Password reset — success/sent confirmation state', () => {
  test.use({ viewport: MOBILE });

  test('shows the sent confirmation card after submitting an email', async ({ page }) => {
    const reset = new PasswordResetPage(page);
    await reset.goto();
    await page.waitForLoadState('networkidle');

    // Pre-submit: the success card is not present yet.
    await expect(reset.successCard).toHaveCount(0);

    await reset.requestReset('confirm-state@forgefit.app');

    await expect(reset.successCard).toBeVisible();
    await expect(reset.resendButton).toBeVisible();
  });
});

test.describe('Auth pages — tablet 2-column stretch layout', () => {
  test.use({ viewport: TABLET });

  for (const route of ['/sign-in', '/sign-up', '/password-reset']) {
    test(`${route} renders as a 2-column grid at tablet width`, async ({ page }) => {
      await page.goto(route);
      await page.waitForLoadState('networkidle');

      const root = page.locator('[class$="-page"]').first();
      await expect(root).toBeVisible();

      const display = await root.evaluate((el) => getComputedStyle(el).display);
      expect(display).toBe('grid');

      const tracks = await root.evaluate(
        (el) => getComputedStyle(el).gridTemplateColumns
      );
      // grid-template-columns: 1fr 1fr resolves to "<px> <px>" (two tracks).
      expect(tracks.trim().split(/\s+/).length).toBeGreaterThanOrEqual(2);
    });
  }
});
