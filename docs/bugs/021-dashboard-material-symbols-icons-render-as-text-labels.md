# Bug 021: Material Symbols icons on the dashboard render as raw text labels

## Status
Complete

## Severity
High

## Area
Dashboard

## References
- Implementation: http://localhost:4321/dashboard
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/dashboard.png`

## Description
The dashboard's CTA buttons, badge medals, eating-window icon, today's-sessions equipment icons, and on-track chip all render the literal Material Symbols ligature text (`play_arrow`, `history`, `wb_sunny`, `local_fire`, `nightlight`, `nights_stay`, `directions_run`, `check_circle`) instead of the glyph. The same icons render correctly on `/profile`, `/rewards`, `/workouts`, `/workouts/new`, and `/workout-detail`, so this is dashboard-specific — likely a missing font-family on those `<span>` elements (e.g. `material-symbols-rounded` class used in markup but the stylesheet only ships `material-icons`, or a CSS scoping bug that strips the `font-family` rule inside `dashboard.page.scss`).

## Expected Behavior
- Every icon span on the dashboard renders as its glyph (sun, fire, moon, runner, bike, fasting, check-circle, play, history, gift, etc.).
- No "play_arrow" / "wb_sunny" / "directions_run" text is ever visible to the user.

## Actual Behavior
- Hero CTAs show "play_arrow Start morning workout" and "history View today's sessions".
- Badge row shows "wb_sunny", "local_fire" (truncated), "nightlight" as plain text labels above each badge title.
- Eating-window icon tile shows "nights_stay" as text.
- "On track" chip shows "check_circle" as text before the chip text.
- Today's-sessions rows show "directions_run" as text in the equipment icon tile.

## Proposed Fix
- Audit `dashboard.page.html` for `<span class="material-symbols-rounded">` usages and confirm the global stylesheet actually loads the Material Symbols Rounded font face (`@import url('https://fonts.googleapis.com/icon?family=Material+Symbols+Rounded')` or local `@font-face`). The other pages use `material-icons` which IS loaded; the dashboard markup mixes the two class names.
- Either (a) standardise on `material-icons` everywhere and replace the `material-symbols-rounded` class names in the dashboard template, or (b) load the Material Symbols Rounded font globally in `styles.scss`.
- Add a smoke test that asserts no `<span class*="material"> ` element has visible text content longer than two characters at the dashboard route (a positive guard against ligature-not-rendered).
