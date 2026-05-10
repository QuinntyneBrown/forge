# Bug 007: Sign-in page is not full-screen on desktop

## Status
Complete

## Severity
Medium

## Area
Auth / Sign-in page

## References
- Implementation: http://localhost:4321/sign-in

## Description
On desktop viewports, the sign-in page does not fill the full screen height. There is a large block of white space below the sign-in card / content, leaving the page looking unfinished.

## Steps to Reproduce
1. Open http://localhost:4321/sign-in in a desktop browser (e.g. 1440×900 or larger).
2. Observe the area below the sign-in form.

## Expected Behavior
The sign-in page should always occupy the full viewport on desktop. The background (gradient / image / solid color, per the mock) should extend to the bottom of the screen with no white gap. Vertical centering of the sign-in card within the viewport is acceptable; trailing white space is not.

## Actual Behavior
The page content stops short of the viewport height, leaving a large white area at the bottom.

## Proposed Fix
- Ensure the sign-in route's host container uses `min-height: 100vh` (or `100dvh`) and that any parent layout wrappers do not constrain it.
- Apply the page background to the full-height container so it extends to the bottom of the viewport.
- Verify behavior across common desktop breakpoints and short viewports (no overflow regressions).

## Notes
Sign-up page should be checked for the same issue while fixing this.
