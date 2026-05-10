# Bug 008: New workout form has wrong colors and is missing sections

## Status
Complete

## Severity
High

## Area
Workouts / New workout page

## References
- Implementation: http://localhost:4321/workouts/new
- Design mock: `file:///C:/projects/forge/docs/mocks/workout-detail.html`

## Description
The form on the new-workout / session page has incorrect colors compared to the mock, and is missing two sections that the mock specifies below the session details form.

## Expected Behavior
Per `docs/mocks/workout-detail.html`:
- The page background should be **white** on this screen.
- Below the "Session details" form there should be additional sections:
  - **Points Breakdown** — showing how points are calculated for the session.
  - **Actions** — the action buttons / controls section.

## Actual Behavior
- Background and form colors do not match the mock (off / tinted instead of white).
- Both the "Points Breakdown" and "Actions" sections are missing from below the form.

## Proposed Fix
- Set the page / form background to white to match the mock.
- Audit the form's surface, border, and input colors against the mock and align them.
- Add the "Points Breakdown" section below the session details, populated from the same data driving the points logic.
- Add the "Actions" section below the form, matching the mock's layout and button styling.
- Run a UI audit (`/ui-audit`) against `/workouts/new` once changes land.

## Notes
This builds on Bug 005 (overall mock alignment for `/workouts/new`). Per-element pixel polish remains tracked under the broader UI-audit pass.
