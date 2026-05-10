// Acceptance Test for Bug 019
// Description: The bottom navigation strip must stay pinned to the bottom
// edge of the viewport at all times on mobile and tablet viewports — both
// before scrolling AND after scrolling the page (or any inner scroll
// container) to its bottom. This is distinct from Bug 015, which was about
// content padding. Bug 019 is specifically about the nav's `position`
// actually being `fixed`/`sticky` and the containing block being the
// viewport (no `transform`/`filter`/`perspective` ancestor stealing it).
//
// Verified at:
//   - mobile (390x844) — /dashboard, /workouts
//   - tablet (834x1112) — /dashboard, /workouts
//
// Uses the seeded dev account so /workouts has rows that overflow.

import { Browser, expect, test } from '@playwright/test';
import { BottomNavPage } from '../pages/bottom-nav.page';
import { DashboardPage } from '../pages/dashboard.page';
import { SignInPage } from '../pages/sign-in.page';

const DEV_EMAIL = 'dev@forge.local';
const DEV_PASSWORD = 'DevPassword123!';

const VIEWPORTS = [
  { label: 'mobile', width: 390, height: 844 },
  { label: 'tablet', width: 834, height: 1112 }
] as const;

const ROUTES = ['/dashboard', '/workouts'] as const;

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

test.describe('Bug 019: bottom nav stays pinned to viewport bottom', () => {
  for (const vp of VIEWPORTS) {
    for (const route of ROUTES) {
      test(`${vp.label} (${vp.width}x${vp.height}) — ${route}: nav pinned before and after scroll`, async ({
        browser
      }) => {
        const { ctx, page } = await signInAsDev(browser, {
          width: vp.width,
          height: vp.height
        });
        const nav = new BottomNavPage(page);

        if (route !== '/dashboard') {
          await page.goto(route);
          await page.waitForLoadState('networkidle');
        }
        await expect(nav.nav).toBeVisible();

        // 1. Source of truth: nav.bottom must equal viewport.height
        //    BEFORE any scroll.
        await nav.expectPinnedToBottom(vp.height);

        // 2. Computed style: the positioned ancestor (the <forge-bottom-nav>
        //    host) must be `fixed` or `sticky` — never `static` or
        //    `relative`. This protects against a regression where the
        //    cascade or Angular view-encapsulation drops the rule.
        const hostPosition = await page.evaluate(() => {
          const host = document.querySelector('forge-bottom-nav');
          return host ? getComputedStyle(host).position : 'missing';
        });
        expect(['fixed', 'sticky']).toContain(hostPosition);

        // 3. No ancestor of the bottom nav may have a non-`none`
        //    `transform`/`filter`/`perspective` — those promote the
        //    ancestor to the containing block of `position: fixed`
        //    descendants and would silently break the pin. (Sticky is
        //    less affected, but we lock both invariants.)
        const offendingAncestor = await page.evaluate(() => {
          const host = document.querySelector('forge-bottom-nav');
          if (!host) return 'missing';
          let el: Element | null = host.parentElement;
          while (el && el !== document.documentElement) {
            const cs = getComputedStyle(el);
            if (
              cs.transform !== 'none' ||
              cs.filter !== 'none' ||
              cs.perspective !== 'none'
            ) {
              return `${el.tagName.toLowerCase()}.${el.className}: ` +
                `transform=${cs.transform} filter=${cs.filter} perspective=${cs.perspective}`;
            }
            el = el.parentElement;
          }
          return null;
        });
        expect(offendingAncestor).toBeNull();

        // 4. After scrolling to the very bottom of the page (and any
        //    internal scroll container), the nav must STILL be pinned.
        await nav.scrollToBottom();
        await nav.expectPinnedToBottom(vp.height);

        await ctx.close();
      });
    }
  }
});
