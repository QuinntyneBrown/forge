# Bug 031: Dashboard is missing the fixed orange "Log workout" FAB — uses an inline pill in the Today's sessions card header instead

## Status
Complete — `dashboard-log-workout-fab.spec.ts` asserts the FAB renders, has computed `position: fixed`, and that no inline "Log workout" button lives inside the Today's sessions card. The fix shipped earlier (the dashboard already mounts the fixed FAB and the inline pill was removed); this iteration locks the behavior in via the new regression-guard spec. Note: the FAB renders in primary teal (per Bug 009's `--mat-sys-primary` pin), not the mock's secondary orange — the teal contract from Bug 009 wins because its e2e asserts the FAB background distance to `#106B5C`.

## Severity
Low

## Area
Dashboard

## References
- Implementation: http://localhost:4321/dashboard
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html` (lines 121–123, 298)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/dashboard.png`
- Related (closed): `docs/bugs/026-workouts-list-replace-new-session-pill-with-orange-fab.md`

## Description
Bug 026 added the orange `+ Log workout` FAB to the workouts list. The same FAB is specified on the dashboard mock (it is `position: fixed` in the global app shell, not scoped to a single route) but the dashboard implementation does not render it. Instead, an inline `+ Log workout` pill is placed in the right-edge of the `Today's sessions` card title row. This is visible across desktop, tablet, and mobile dashboard screenshots.

## Expected Behavior
Per `docs/mocks/dashboard.html`:
- A fixed-position `Log workout` FAB anchored at the bottom-right of the viewport (`right: 16px; bottom: 88px` on mobile/tablet to clear the bottom nav, `right: 32px; bottom: 32px` at the desktop breakpoint).
- Background `var(--md-sys-color-secondary)` (orange `#B8531A`), white text, `add` glyph leading the label, `var(--md-elevation-3)` shadow, `var(--shape-lg)` corner radius.
- Same FAB renders consistently across dashboard, workouts list, and any other authenticated route per the shell pattern.

## Actual Behavior
- No fixed FAB on the dashboard.
- Instead, an orange pill `+ Log workout` is rendered inside the `Today's sessions` card, top-right of the card title — visually competing with the card eyebrow / heading and easy to miss because it scrolls with the card.

## Proposed Fix
- Hoist the `LogWorkoutFabComponent` from the workouts page into the authenticated app shell so it renders on every route the mock calls for (dashboard, workouts, etc.). Hide it on routes where the mock does not show a FAB (sign-in, sign-up, password-reset, error-state, profile, rewards — verify each against its mock).
- Remove the inline `+ Log workout` pill from the dashboard `Today's sessions` card header; the card heading should match the mock's plain `2 workouts logged` title with no trailing action.
- Keep the click handler — it should navigate to `/workouts/new` from any route.
