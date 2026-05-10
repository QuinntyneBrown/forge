# Bug 019: Bottom navigation does not stay pinned to the viewport bottom on short pages

## Status
Complete

## Severity
Medium

## Area
App shell / bottom navigation

## References
- Implementation: http://localhost:4321/{dashboard,profile,workouts/new,workout-detail}
- Design mocks: `file:///C:/projects/forge/docs/mocks/dashboard.html` (and other authenticated mocks — `.bottomnav { position: fixed; bottom: 0 }`)
- Screenshots: `docs/screenshots/mobile/{dashboard,profile,workouts-new,workout-detail}.png`, `docs/screenshots/tablet/{dashboard,workouts-new,rewards,workouts}.png`

## Description
On the mobile and tablet screenshots the bottom navigation is rendered **mid-page** (over the dashboard's Tier card, over the profile's Daily-workout-minutes field, over the workout form's Avg HR field, etc.) instead of being pinned to the viewport bottom. The mocks all declare `.bottomnav { position: fixed; left: 0; right: 0; bottom: 0; z-index: 15 }`. The implementation appears to render the nav as an in-flow element (or the layout's scroll container is not the viewport, so `position: fixed` is anchoring relative to the wrong element).

This is distinct from Bug 015 (content-padding) — even after Bug 015 reserves space, the nav itself is in the wrong position.

## Expected Behavior
- The bottom nav stays glued to the bottom edge of the viewport at all times.
- On short pages the area above the nav is the page background — it does not appear in the middle of the page content.
- On long pages the nav stays pinned while the page content scrolls beneath it.

## Actual Behavior
- The nav appears at a fixed offset from the top of the page (looks like ~roughly the height of the smallest viewport tested), so on taller content it ends up mid-page.
- This compounds Bug 015 because users see two "below the nav" regions instead of one.

## Proposed Fix
- Verify the bottom-nav element is a child of `body` (or of a top-level shell whose containing block is the viewport).
- If using a CDK `cdk-scrollable` or a layout component with its own scroll container, switch the bottom nav to `position: fixed` outside that container, or use `position: sticky; bottom: 0` on a flex-shrink:0 child of the scroll container.
- Add a layout test that opens each route at mobile + tablet and asserts the nav's `getBoundingClientRect().bottom === window.innerHeight`.

## Resolution

No code change required — resolved as a side-effect of Bug 015.

When Bug 015 reorganised the app-shell into a flex column (`min-height: 100vh`) with an inner `overflow:auto` main scroll container and the `<forge-bottom-nav>` as a sibling sticky child of the shell (`frontend/projects/components/src/lib/app-shell/app-shell.component.scss:1-105` and `frontend/projects/components/src/lib/bottom-nav/bottom-nav.component.scss:4-9`), the bottom-nav host's `position: sticky; bottom: 0` started resolving correctly — the shell's containing block IS the viewport at <992px, no ancestor introduces a `transform`/`filter`/`perspective`, and the inner main reserves `--bottom-nav-safe-area` so content never extends under the nav.

Verified at mobile (390x844) and tablet (834x1112) on `/dashboard` and `/workouts`: nav.bottom === viewport.height both before AND after scrolling to the bottom of the page.

A regression-lock acceptance test was added at `frontend/e2e/tests/bottom-nav-pinned.spec.ts` (uses the existing `frontend/e2e/pages/bottom-nav.page.ts` POM with a new `expectPinnedToBottom(viewportHeight)` helper). The spec also asserts the host's computed `position` is `fixed`/`sticky` and that no ancestor has a `transform`/`filter`/`perspective` that would silently steal the containing block of `position: fixed` descendants.
