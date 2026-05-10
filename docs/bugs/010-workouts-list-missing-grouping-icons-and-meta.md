# Bug 010: Workouts list is missing day grouping, equipment icons, meta icons, points and times

## Status
Complete

## Severity
High

## Area
Workouts list

## References
- Implementation: http://localhost:4321/workouts
- Design mock: `file:///C:/projects/forge/docs/mocks/workouts.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/workouts.png`

## Description
The implemented `/workouts` route renders a flat, ungrouped list of monochrome session rows containing only "Treadmill / 29 min · 657 kcal" style text. The mock specifies a much richer structure with grouped day headers, per-equipment colored icon tiles, an icon-prefixed metadata row, and a right-side points + time stack. None of that structure is present.

## Expected Behavior
Per `docs/mocks/workouts.html`:
- A "Sessions" page header with a subtitle ("12 sessions · 6 h 14 min · 4,820 cal this week").
- A 3-column "summary strip" showing This week minutes, Calories, Points earned (with delta vs last week).
- A horizontal scrollable filter chip row: All / Treadmill / Bike / Bench / Elliptical / This week, with the selected chip in the secondary-container tonal style.
- Sessions grouped by day with uppercase eyebrow headers ("Today · Tuesday, May 12", "Yesterday · Monday, May 11", etc.).
- Each session row: 48×48 rounded icon tile in an equipment-specific color (green for treadmill, amber for bike, orange for bench, blue for elliptical), title, an icon-prefixed meta row (timer + duration, flame + cal, distance/scale/HR), and a right-aligned `+85 pts` (in secondary orange) above a small time-of-day caption.
- A "+ Log workout" FAB pinned bottom-right (orange / secondary).

## Actual Behavior
- No page header, no summary strip, no day-group headers.
- Filter chips ARE present (good) but layered above content correctly only at desktop.
- Session rows are plain bordered cards with equipment name, duration · kcal — no icon tile, no meta-row icons, no points, no time.
- There is a "New session" pill button top-right but no orange FAB.

## Proposed Fix
- Build a `WorkoutListItemComponent` that renders an icon tile (color keyed off equipment), the title, an icon-prefixed meta `<dl>`, and a points/time stack on the right.
- Group the session feed by day in the parent component (`Today`, `Yesterday`, then `Weekday, Mon dd`).
- Add the summary-strip and page-header components above the filters.
- Replace the top-right "New session" CTA with the standard orange FAB used on the dashboard.

## Resolution
Initial structural rebuild landed in commit `77d013a` (Sessions header,
3-tile summary strip, day-grouped rows, equipment-tinted icon tiles,
icon-prefixed meta, points/time stack).

Follow-up ATDD pass added the granular content + a11y guarantees that
`docs/mocks/workouts.html` calls out:

- `frontend/projects/domain/src/lib/workout-list/workout-list.component.ts`
  - Added `EQUIPMENT_LABEL` and `labelFor()` so the equipment chip and
    icon tile present a friendly label ("Indoor bike" instead of
    "IndoorBike").
- `frontend/projects/domain/src/lib/workout-list/workout-list.component.html`
  - `data-testid="workout-list-day-group-label"` on each eyebrow header.
  - Equipment icon tile now has `role="img"` + an `aria-label` naming
    the equipment (was `aria-hidden`).
  - Meta row gained `data-testid` per item (`-duration`, `-calories`,
    and conditional `-distance` / `-hr` rendered when the session has a
    distance or heart-rate value, mirroring the mock's straighten and
    favorite glyphs).
  - Right stack now exposes `workout-list-row-points-value`
    (`+N pts`, full text incl. unit) and `workout-list-row-time`
    testids.
- `frontend/e2e/pages/workouts-list.page.ts` — extended POM with
  `pageTitle`, `pageSubtitle`, `summaryStrip`, `summaryStat`,
  `dayGroups`, `dayGroupHeaders`, `sessionRow`, `sessionRowEquipmentIcon`,
  `sessionRowMetaIcon`, `sessionRowPoints`, `sessionRowTime`.
- `frontend/e2e/tests/workouts-list-content-and-styling.spec.ts` — new
  acceptance suite (5 cases) covering the summary strip, ≥2 day-group
  headers, icon tile a11y label, meta-row icon entries, and the
  `+N pts` / time-of-day stack.
