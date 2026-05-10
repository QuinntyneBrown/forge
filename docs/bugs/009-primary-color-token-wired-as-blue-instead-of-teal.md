# Bug 009: Primary color token wired as blue instead of mock teal across the app

## Status
Complete

## Severity
High

## Area
Theming / design tokens (app-wide)

## References
- Implementation: http://localhost:4321/ (any authenticated route)
- Design mocks: `file:///C:/projects/forge/docs/mocks/dashboard.html`, `rewards.html`, `workout-detail.html`, `profile.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/{dashboard,profile,workouts-new,workout-detail,rewards}.png`

## Description
Every mock declares `--md-sys-color-primary: #106B5C` (forest/teal) and renders all filled primary buttons, the dashboard calorie ring, the rewards "Redeem" buttons, the workout-detail "Save" buttons, and the profile "Save changes" button in that teal. The implemented app instead renders all of these in a saturated material blue (looks roughly `#1976d2`). This is a single design-token wiring issue that cascades through nearly every screen, but it is highly visible and mock-breaking everywhere.

## Expected Behavior
- Filled primary buttons (Save changes, Save session, Send reset link, Sign in, Create account, Redeem, Go to dashboard) render in `#106B5C`.
- Calorie ring foreground stroke uses the mock's accent (peach `#FFCBA1` on the green hero) — currently it is also drawn in blue.
- "Sign out" outline button uses the primary teal stroke + label, not blue.
- Active nav-rail item label / Profile active state use the teal token, not blue.

## Actual Behavior
- Filled primary buttons render in blue.
- Calorie ring fill renders in blue on the dashboard.
- "Sign out" outline button renders in blue.
- Active rail item ("Workouts") label renders in blue.

## Proposed Fix
- In the Angular Material theme / SCSS palette, replace the default `mat.$indigo-palette` (or whichever blue palette is currently configured) with a custom palette built around `#106B5C` (per the M3 token map in the mocks).
- Audit all hard-coded `mat-mdc-button.mat-primary` overrides and any custom CSS using `--mat-sys-primary` to ensure they pick up the new palette.
- Snapshot-verify dashboard / rewards / profile / workouts-new after the change.
