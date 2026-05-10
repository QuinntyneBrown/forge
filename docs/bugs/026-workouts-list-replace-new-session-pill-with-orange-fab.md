# Bug 026: Workouts list still shows top-right "New session" pill instead of the orange "+ Log workout" FAB

## Status
Complete

## Severity
Low

## Area
Workouts list

## References
- Implementation: http://localhost:4321/workouts
- Design mock: `file:///C:/projects/forge/docs/mocks/workouts.html` (lines 64–65 + 251 — `.fab { position: fixed; right: 16px; bottom: 88px; background: var(--md-sys-color-secondary) }`)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/workouts.png`
- Prior bug: `docs/bugs/010-workouts-list-missing-grouping-icons-and-meta.md` (closed; Proposed Fix explicitly called for replacing the top-right CTA with the dashboard FAB — that swap was not done)

## Description
The Sessions page header still ends with a `New session` outlined pill button on the top-right corner. The mock specifies a single orange (secondary container) extended FAB labelled `+ Log workout` pinned to the bottom-right of the viewport, exactly the same pattern as the dashboard. Two CTAs to "create a session" is redundant and the pill in the header is the wrong one to keep.

## Expected Behavior
- Remove the `New session` pill from the page header. The page header is just the title + subtitle + summary strip.
- Add a fixed `+ Log workout` FAB pinned to the bottom-right at mobile/tablet (offset to clear the bottom nav, e.g. `bottom: 88px`) and to `bottom: 32px / right: 32px` at desktop.
- FAB uses the secondary (orange) palette: `background: var(--md-sys-color-secondary); color: #fff` per mock.
- On click it routes to `/workouts/new`.

## Actual Behavior
- Header has a small outlined teal `New session` pill on the top-right.
- No FAB anywhere on the page.

## Proposed Fix
- Reuse the existing dashboard FAB component (or extract it into `frontend/projects/components/src/lib/fab/`) and render it in the workouts-list template.
- Delete the `New session` button from the header.
- Confirm the FAB uses `--mat-sys-secondary-container` / orange tone per the design tokens already wired up by Bug 009.
