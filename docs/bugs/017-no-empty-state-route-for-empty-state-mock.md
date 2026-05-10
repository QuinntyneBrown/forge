# Bug 017: Empty-state mock has no implemented route or component

## Status
Complete

## Severity
Medium

## Area
Workouts / first-run experience

## References
- Implementation: no route exists
- Design mock: `file:///C:/projects/forge/docs/mocks/empty-state.html`
- Screenshots: not applicable — there is nothing to capture

## Description
`docs/mocks/empty-state.html` is the first-run version of the workouts list ("No workouts logged — yet."). It includes a hero illustration, a "Day 1" badge, two CTAs, a 4-up equipment picker grid, and a "Pro tip" tonal card. None of this exists in the implementation:
- There is no dedicated `/empty-state` showcase route to view it directly.
- The actual `/workouts` route does not branch into an empty state when the session list is empty — it just renders the filter chips above an empty card area, with no illustration, no CTAs, no equipment picker, and no pro tip.

## Expected Behavior
- When `/workouts` has zero sessions for the active filter, render the empty-state layout from the mock:
  - Round gradient illustration (`fitness_center` icon in a white rounded square, dashed circle outline, "Day 1" / contextual badge top-right).
  - Title "No workouts logged — yet." (or filter-aware copy).
  - One-paragraph sub explaining the gamification carrot.
  - Two CTAs: filled teal "Log your first workout" + text "Take me back home".
  - 4-tile equipment picker grid (Treadmill / Indoor bike / Bench press / Elliptical) that deep-links to `/workouts/new?equipment=…`.
  - A teal tonal "Pro tip — log before 7 AM for the morning bonus" card at the bottom.
- The "Day 1" badge text should be derived from the user's actual streak day count.

## Actual Behavior
- The empty branch is not implemented; the workouts page on a fresh account would just look broken.
- There is no way to preview the empty layout from inside the running app, so design review of this state has been skipped.

## Proposed Fix
- Build an `EmptyWorkoutsComponent` that matches the mock and render it from `WorkoutsPageComponent` when `sessions().length === 0` (or when filters yield zero rows — slightly different copy).
- Consider adding an `/empty-state` debug route behind a dev flag so this state stays reviewable in QA without seeding/clearing data.
