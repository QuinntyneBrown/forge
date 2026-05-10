# Bug 030: Workout-detail edit page Points breakdown is collapsed to Base + Subtotal — missing multiplier rows and Total earned pill

## Status
Open

## Severity
Medium

## Area
Workouts / Edit session

## References
- Implementation: http://localhost:4321/workouts/:id (existing session edit)
- Design mock: `file:///C:/projects/forge/docs/mocks/workout-detail.html` (lines 55–62, 172–178)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/workout-detail.png`
- Related (closed): `docs/bugs/014-workout-detail-edit-missing-hero-and-actions.md`

## Description
Bug 014 restored the hero and Actions card on the existing-session edit page, but the Points breakdown card was only partially rebuilt. It currently shows two rows — `Base · 29 min logged · +58` and `Subtotal (base) · +58` — and nothing else. The mock specifies four breakdown rows (Base, Morning bonus, Zone 2 consistency, Streak multiplier), each with its own leading icon, capped by a teal `Total earned` pill in `--md-sys-color-primary-container`. The new-workout page (`/workouts/new`) already renders this complete breakdown correctly, so the gap is isolated to the edit-session view.

## Expected Behavior
Per `docs/mocks/workout-detail.html`:
- Four `.points__row` entries with leading Material Symbols icons:
  - `timer` — Base — N min logged — `+pts`
  - `wb_sunny` — Morning bonus (before 7 AM) — `+pts` (when applicable)
  - `favorite` — Zone 2 consistency — `+pts` (when avg HR is in zone 2)
  - `trending_up` — Streak multiplier (×N.NN) — `+pts`
- Final `.points__total` pill with `--md-sys-color-primary-container` background, `--md-sys-color-on-primary-container` text, showing `Total earned` on the left and `+NN pts` on the right at 18px / 700.
- Rows whose value is zero (e.g. no morning bonus) may be omitted, but the Total pill is always present.

## Actual Behavior
- Only two rows render: `Base — 2 pts per minute logged` (or `Base · N min logged`) and `Subtotal (base)`.
- No morning bonus, zone-2, or streak-multiplier rows ever render, even on a 5:12 AM treadmill session that should award all three.
- No highlighted Total earned pill — the bottom line is just another grey row.

## Proposed Fix
- Reuse the same `PointsBreakdownComponent` (or template) the new-workout page uses on the edit-session page; the data shape coming back from the session API already includes the multipliers.
- If the API response is shaped differently for edit vs new, normalize it server-side or in a small adapter so a single component can render both.
- Render the `.points__total` pill from the computed total (sum of the rows) rather than relying on a separate `subtotal` field.
