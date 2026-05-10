# Bug 030: Workout-detail edit page Points breakdown is collapsed to Base + Subtotal — missing multiplier rows and Total earned pill

## Status
Complete

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

## Resolution
Expanded the existing shared `WorkoutPointsBreakdownComponent`
(`frontend/projects/domain/src/lib/workout-points-breakdown/`) to compute and
render Morning bonus (when `startedAt < 7 AM`), Zone 2 consistency (when avg
HR is 110-145 bpm), and Streak multiplier rows in addition to the Base row.
Each row carries `data-testid="workout-points-breakdown-row"` with a leading
Material Icons glyph, and a highlighted Total earned pill
(`data-testid="workout-points-breakdown-total-pill"`) in
`--md-sys-color-primary-container` sums the rows. The breakdown is computed
client-side from the existing `Session` DTO, so no backend change was
required. `/workouts/:id` now mirrors the `/workouts/new` Points card.

Phase 1 acceptance test: `frontend/e2e/tests/workout-detail-points-breakdown.spec.ts`
(commit `013f0e2`). Phase 2 fix: commit `e444d46`.
