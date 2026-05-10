# Bug 005: New workout page does not align with design mock

## Severity
High

## Area
Workouts / New workout page

## References
- Implementation: http://localhost:4321/workouts/new
- Design mock: `file:///C:/projects/forge/docs/mocks/workout-detail.html`

## Description
The implemented `/workouts/new` page does not align with the authoritative design mock at `docs/mocks/workout-detail.html`. Layout, components, and styling diverge from the design.

## Expected Behavior
The implemented new-workout page should match `docs/mocks/workout-detail.html` in layout, typography, color, spacing, and component composition.

## Proposed Fix
- Compare the implemented page against the mock and enumerate every deviation (header, form layout, set/rep inputs, rest timer, save/cancel actions, etc.).
- Update the implementation to match the mock.
- Run a UI audit (`/ui-audit`) against the page once changes land.

## Notes
A detailed enumeration of deviations is needed — this bug should be expanded with a per-element checklist after a side-by-side audit.
