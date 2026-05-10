# Bug 015: Mobile bottom navigation overlaps page content on multiple screens

## Status
Complete

## Severity
High

## Area
Responsive layout / app shell (mobile + tablet)

## References
- Implementation: http://localhost:4321/{dashboard,workouts,workouts/new,workouts/:id,profile,rewards}
- Design mocks: `file:///C:/projects/forge/docs/mocks/dashboard.html` (and all other authenticated mocks)
- Screenshots: `docs/screenshots/mobile/{dashboard,workouts,workouts-new,workout-detail,profile,rewards}.png`, `docs/screenshots/tablet/{workouts,workouts-new,rewards}.png`

## Description
The fixed bottom navigation bar covers the bottom of the page content on every authenticated mobile screen and on most tablet screens. The mocks reserve `padding-bottom: 96px` on the `.content` element specifically so the last card / last form field / last list row does not sit under the bar. The implementation does not reserve this space.

## Expected Behavior
- On viewports that show the bottom nav (mobile + tablet, i.e. `<1100px`), the page content has enough bottom padding (≈96px) so that scrolling all the way down reveals the last element fully above the bottom nav.
- The bottom nav itself is a solid surface so even when content scrolls under it, nothing important is partially obscured at the resting scroll position.

## Actual Behavior
The bottom nav is rendered as a fixed bar with no corresponding bottom padding on the page content, producing the following visible defects:
- **Mobile dashboard**: the Leaderboard card (the last card) is half-covered by the bottom nav at first paint — only the heading row peeks below it.
- **Mobile / tablet workouts list**: the last 1–2 session rows are clipped by the bottom nav with no scroll buffer.
- **Mobile / tablet workouts/new**: the bottom nav covers the Avg HR / Active calories fields and the Notes textarea; the Save session button is partially behind the nav until the user scrolls past it.
- **Mobile profile**: the Daily workout minutes target field is partially under the nav.
- **Mobile rewards**: the catalog rows scroll behind the nav with no padding buffer.

## Proposed Fix
- In the app shell layout, when the bottom-nav variant is active, set `padding-bottom: 96px` (or `calc(96px + env(safe-area-inset-bottom))`) on the main content scroll container.
- Verify the same on tablet — the nav rail only kicks in at `>=1100px` per the mock breakpoints; tablet (768–1099px) still uses the bottom nav.
- Add a visual regression snapshot at mobile + tablet for each authenticated route once fixed.

## Resolution
Shell-level fix. The bottom inset is now reserved once on the
`<main class="app-shell__main">` container and removed from each page.

- `frontend/projects/forge/src/styles.scss` (added):
  defines the global token
  `--bottom-nav-safe-area: calc(64px + env(safe-area-inset-bottom, 0px))`
  matching the bottom-nav `min-height: 64px` plus the device safe-area.
- `frontend/projects/components/src/lib/app-shell/app-shell.component.html`
  and `.scss`: when `showBottomNav()` is true (i.e. `<992px`,
  matching the existing `RAIL_BREAKPOINT`), the main scroll container
  gets the modifier class `.app-shell__main--with-bottom-nav` which
  applies `padding-bottom: var(--bottom-nav-safe-area, 64px)`. On
  desktop (>=992px) the nav-rail is shown instead and no bottom inset
  is applied.
- Removed the duplicated `padding-bottom: 96px` magic numbers from the
  per-page scss files (`dashboard`, `workouts`, `workout-new`,
  `workout-detail`, `profile`, `rewards`, `error`). Page authors no
  longer need to remember this.

Verified by `frontend/e2e/tests/bottom-nav-overlap.spec.ts` (4 passing
checks: architectural assertion + last-element-visible on /dashboard,
/workouts, /rewards at mobile viewport 390x844).
