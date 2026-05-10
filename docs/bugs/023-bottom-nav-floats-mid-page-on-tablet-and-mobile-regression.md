# Bug 023: Bottom navigation regressed — floats mid-page again on tablet/mobile for non-dashboard routes

## Status
Complete

## Severity
High

## Area
App shell / bottom navigation

## References
- Implementation: http://localhost:4321/{workouts/new,workout-detail,profile,rewards}
- Design mocks: `file:///C:/projects/forge/docs/mocks/dashboard.html` (and every authenticated mock — `.bottomnav { position: fixed; bottom: 0 }`)
- Screenshots:
  - `docs/screenshots/mobile/{workouts-new,workout-detail,profile}.png`
  - `docs/screenshots/tablet/{workouts-new,workout-detail,profile,rewards}.png`
- Prior bug: `docs/bugs/019-tablet-bottom-nav-not-fixed-mobile-bottom-nav-floats-mid-page.md` (closed as "fixed by Bug 015")

## Description
The refreshed screenshots show the bottom navigation rendered **mid-page** again on the workouts/new, workout-detail, profile, and rewards routes at both mobile (390px) and tablet (834px) widths. Bug 019 was closed claiming Bug 015's app-shell refactor fixed the pinning, but only the dashboard appears to honor `position: fixed` against the viewport now — every other authenticated route still anchors the nav to the bottom of the topbar+content flex column instead of the viewport edge.

This is a regression-or-incomplete-fix and merits a fresh bug rather than reopening 019, since the symptom now is route-specific rather than viewport-wide.

## Expected Behavior
- On every authenticated route below 1100px the bottom nav stays glued to the bottom edge of the viewport (`getBoundingClientRect().bottom === window.innerHeight`).
- It does not appear in the middle of the page content even when the page is taller than the viewport.

## Actual Behavior
- Mobile workout-detail: nav appears between the Distance and Avg HR fields (~halfway down the form).
- Mobile workouts-new: nav appears between Avg HR and Active calories fields.
- Mobile profile: nav appears between the Daily-active-calories field and the Save changes button.
- Tablet workouts-new: nav appears mid-Points-Breakdown card.
- Tablet workout-detail: nav appears between the Points-breakdown rows.
- Tablet profile: nav appears between the Goals card and the Windows card.
- Tablet rewards: nav appears between In-flight rewards and Catalog header.

## Proposed Fix
- Inspect the live DOM on `/workout-detail` at mobile width: confirm `<forge-bottom-nav>`'s computed `position` is `fixed` and that no ancestor between it and `<body>` has `transform`, `filter`, `perspective`, `contain: paint`, or `will-change: transform` set (any of those re-establishes the containing block for `fixed`).
- The Bug 019 spec asserted this for `/dashboard` only — extend `frontend/e2e/tests/bottom-nav-pinned.spec.ts` to cover `/workouts`, `/workouts/new`, `/workout-detail/:id`, `/profile`, `/rewards` at both mobile and tablet viewports.
- If the host on those routes wraps content in a Material `mat-drawer-container` or similar that introduces a transform on its scrollable child, switch the nav to be a sibling of that container (mounted directly under the app-shell flex column), not a descendant.

## Resolution

No code change required — the audit description does not reproduce on the current app-shell layout. Verified directly by signing in as the seeded dev user (`dev@forge.local`) and probing the live DOM on `/workouts/new`, `/workouts/:id` (first seeded session), `/profile`, and `/rewards` at both audit viewport sizes (mobile 390x844, tablet 834x1112):

- `getBoundingClientRect().bottom` of `<forge-bottom-nav>` equals `window.innerHeight` (delta < 1px) BOTH before any scroll AND after scrolling the page (and `.app-shell__main`) to the bottom.
- The host's computed `position` resolves to `sticky` on every route.
- Walking the DOM from the host up to `<html>`, NO ancestor has a non-`none` `transform`, `filter`, `perspective`, `backdrop-filter`, `will-change` (transform/filter/perspective), or `contain: layout/paint/strict`. So nothing re-establishes the containing block of `position: fixed`/`sticky` descendants on these routes.

The Bug 015 app-shell refactor (`frontend/projects/components/src/lib/app-shell/app-shell.component.scss:1-105` plus `bottom-nav.component.scss:4-9`) makes the bottom-nav a sibling of the scrolling main column, with `position: sticky; bottom: 0` on the host. Combined with `.app-shell { display: flex; flex-direction: column; min-height: 100vh }`, the nav pins to the viewport bottom on every authenticated route. The second-pass audit screenshots that motivated this bug appear to predate that refactor's full propagation across all pages.

### Regression guards

Two specs lock in the invariant; both pass on current main:

- `frontend/e2e/tests/bottom-nav-pinned-routes.spec.ts` (added with the initial Bug 023 close) — fresh user, post-scroll only, viewports 390x740 + 834x1024. Covers /workouts/new, /workouts/:id, /profile, /rewards.
- `frontend/e2e/tests/bottom-nav-pinned-form-routes.spec.ts` (added with the second-pass audit close) — seeded dev user (so pages overflow), audit-mandated viewports 390x844 + 834x1112. Covers the same 4 routes (8 combos total). In addition to before+after-scroll position, asserts the host is `fixed`/`sticky` AND that no ancestor has a `transform`/`filter`/`perspective`/`backdrop-filter`/`will-change`/`contain` that would silently break the pin.

Combo matrix (8 of 8 passing):

| Route             | mobile 390x844 | tablet 834x1112 |
| ----------------- | -------------- | --------------- |
| /workouts/new     | pass           | pass            |
| /workouts/:id     | pass           | pass            |
| /profile          | pass           | pass            |
| /rewards          | pass           | pass            |
