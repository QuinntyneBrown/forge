// Acceptance Test for Bug 023 — second-pass audit
//
// Description: Bug 019 was closed when Bug 015's app-shell refactor moved
// the bottom nav to a sibling of the scrolling main column. The second-pass
// audit reported the nav floating mid-page again on the form-heavy routes
// (/workouts/new, /workouts/:id, /profile, /rewards) at mobile (390x844)
// and tablet (834x1112) widths — but only with the dev user (which has
// seeded sessions making the pages tall enough to scroll).
//
// A sibling regression guard `bottom-nav-pinned-routes.spec.ts` already
// exists, but it (a) provisions a fresh user with no seeded sessions, so
// the pages are short and never exercise the long-scroll path, and (b)
// only asserts post-scroll position. This spec expands coverage:
//
//   - Signs in as the seeded dev user so the form pages overflow.
//   - Uses the spec-mandated viewport sizes (390x844 mobile, 834x1112
//     tablet) — the audit was done at these sizes.
//   - Asserts nav.bottom === viewport.height BEFORE and AFTER scrolling
//     to the bottom of the page (and any inner scroll container).
//   - Asserts no ancestor between <forge-bottom-nav> and <body> has a
//     non-`none` `transform`, `filter`, `perspective`, `backdrop-filter`,
//     `will-change` (for any of those), or `contain: layout/paint`
//     property — any of which would re-establish the containing block of
//     `position: fixed`/`sticky` descendants and silently break the pin.
//
// Covers 8 (route x viewport) combos. Fails fast per combo via describe-
// grouped tests.

import { Browser, expect, test } from '@playwright/test';
import { BottomNavPage } from '../pages/bottom-nav.page';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';
import { WorkoutsListPage } from '../pages/workouts-list.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';

const VIEWPORTS = [
  { label: 'mobile', width: 390, height: 844 },
  { label: 'tablet', width: 834, height: 1112 }
] as const;

type StaticRoute = '/workouts/new' | '/profile' | '/rewards';
const STATIC_ROUTES: readonly StaticRoute[] = ['/workouts/new', '/profile', '/rewards'];

async function signInAsDev(
  browser: Browser,
  viewport: { width: number; height: number }
) {
  const ctx = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport
  });
  const page = await ctx.newPage();
  const signIn = new SignInPage(page);
  const dashboard = new DashboardPage(page);
  await signIn.goto();
  await signIn.signIn(DEV_EMAIL, DEV_PASSWORD);
  await dashboard.waitForLoad();
  return { ctx, page };
}

async function findFirstSessionId(
  page: import('@playwright/test').Page
): Promise<string> {
  const list = new WorkoutsListPage(page);
  await list.goto();
  await page.waitForLoadState('networkidle');
  const firstRow = page.locator('[data-testid^="workout-list-row-"]').first();
  await expect(firstRow).toBeVisible();
  const testId = await firstRow.getAttribute('data-testid');
  if (!testId) throw new Error('first workout row has no data-testid');
  const id = testId.replace(/^workout-list-row-/, '');
  if (!id) throw new Error(`could not parse session id from "${testId}"`);
  return id;
}

async function assertNoOffendingAncestors(
  page: import('@playwright/test').Page
): Promise<void> {
  const offending = await page.evaluate(() => {
    const host = document.querySelector('forge-bottom-nav');
    if (!host) return 'forge-bottom-nav not found';
    const offenders: string[] = [];
    let el: Element | null = host.parentElement;
    while (el && el !== document.documentElement) {
      const cs = getComputedStyle(el);
      const wc = cs.willChange ?? '';
      const containment = cs.contain ?? '';
      const reasons: string[] = [];
      if (cs.transform && cs.transform !== 'none') reasons.push(`transform=${cs.transform}`);
      if (cs.filter && cs.filter !== 'none') reasons.push(`filter=${cs.filter}`);
      if (cs.perspective && cs.perspective !== 'none') reasons.push(`perspective=${cs.perspective}`);
      // backdrop-filter establishes a containing block per spec.
      const bdf = (cs as unknown as { backdropFilter?: string }).backdropFilter;
      if (bdf && bdf !== 'none') reasons.push(`backdrop-filter=${bdf}`);
      if (
        wc.includes('transform') ||
        wc.includes('filter') ||
        wc.includes('perspective')
      ) {
        reasons.push(`will-change=${wc}`);
      }
      if (containment.includes('paint') || containment.includes('layout') || containment.includes('strict')) {
        reasons.push(`contain=${containment}`);
      }
      if (reasons.length > 0) {
        const tag = el.tagName.toLowerCase();
        const cls = (el as HTMLElement).className || '';
        offenders.push(`<${tag} class="${cls}"> ${reasons.join(', ')}`);
      }
      el = el.parentElement;
    }
    return offenders.length === 0 ? null : offenders.join(' || ');
  });
  expect(
    offending,
    `Found ancestor(s) of <forge-bottom-nav> with a property that ` +
      `re-establishes the containing block of position: fixed/sticky ` +
      `descendants — the nav will float mid-page on this route. ` +
      `Offenders: ${offending}`
  ).toBeNull();
}

async function assertHostPositionFixedOrSticky(
  page: import('@playwright/test').Page
): Promise<void> {
  const hostPosition = await page.evaluate(() => {
    const host = document.querySelector('forge-bottom-nav');
    return host ? getComputedStyle(host).position : 'missing';
  });
  expect(['fixed', 'sticky']).toContain(hostPosition);
}

test.describe('Bug 023: bottom nav stays pinned on form-heavy routes (dev user)', () => {
  for (const vp of VIEWPORTS) {
    for (const route of STATIC_ROUTES) {
      test(`${vp.label} (${vp.width}x${vp.height}) — ${route}: pinned before & after scroll`, async ({
        browser
      }) => {
        const { ctx, page } = await signInAsDev(browser, {
          width: vp.width,
          height: vp.height
        });
        const nav = new BottomNavPage(page);

        await page.goto(route);
        await page.waitForLoadState('networkidle');
        await expect(nav.nav).toBeVisible();

        // Invariant 1: pinned at initial scroll position.
        await nav.expectPinnedToBottom(vp.height);
        // Invariant 2: host is fixed/sticky.
        await assertHostPositionFixedOrSticky(page);
        // Invariant 3: no ancestor steals the containing block.
        await assertNoOffendingAncestors(page);
        // Invariant 4: still pinned after scrolling to the bottom.
        await nav.scrollToBottom();
        await nav.expectPinnedToBottom(vp.height);

        await ctx.close();
      });
    }

    test(`${vp.label} (${vp.width}x${vp.height}) — /workouts/:id: pinned before & after scroll`, async ({
      browser
    }) => {
      const { ctx, page } = await signInAsDev(browser, {
        width: vp.width,
        height: vp.height
      });
      const nav = new BottomNavPage(page);

      const sessionId = await findFirstSessionId(page);
      await page.goto(`/workouts/${sessionId}`);
      await page.waitForLoadState('networkidle');
      await expect(nav.nav).toBeVisible();

      await nav.expectPinnedToBottom(vp.height);
      await assertHostPositionFixedOrSticky(page);
      await assertNoOffendingAncestors(page);
      await nav.scrollToBottom();
      await nav.expectPinnedToBottom(vp.height);

      await ctx.close();
    });
  }
});
