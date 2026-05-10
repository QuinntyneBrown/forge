# Bug 024: Workout-detail Actions card renders an empty circle instead of Duplicate / Delete buttons

## Status
Open

## Severity
High

## Area
Workout detail

## References
- Implementation: http://localhost:4321/workout-detail/:id
- Source: `frontend/projects/forge/src/app/pages/workout-detail/workout-detail.page.html` lines 63–89 (declares `<forge-button>Duplicate</forge-button>` and `<forge-button>Delete session</forge-button>`)
- Screenshots:
  - `docs/screenshots/desktop/workout-detail.png`
  - `docs/screenshots/tablet/workout-detail.png`
  - `docs/screenshots/mobile/workout-detail.png`

## Description
The Actions card on `/workout-detail/:id` renders a single empty oval/circle outline at the top of the card body and nothing else — no "Duplicate" button, no "Delete session" button, no labels. The source clearly declares both buttons. This looks like the `forge-button` component is rendering a button-shaped outline but its projected content (the literal text inside the tag) is being swallowed, OR the Material theme is painting white-on-white.

## Expected Behavior
- Actions card body shows two buttons side-by-side (or stacked at narrow widths):
  - Outlined button labelled `Duplicate`.
  - Text-variant button (or outlined error variant per workout-detail mock convention) labelled `Delete session`.
- Both buttons disabled when `busy() || !session()` is true.
- Below the buttons: the existing info caption "Deleting refunds points and removes from today's totals."

## Actual Behavior
- Single empty rounded outline (looks like one button's chrome but with no label).
- No second button at all.
- Info caption is present below.

## Proposed Fix
- Inspect the rendered DOM under `[data-testid="workout-detail-actions-card"]` to confirm whether the second `<forge-button>` is present in DOM or being filtered out (CDK `*ngIf`?). If both present, the issue is a styling / content-projection bug in `<forge-button>`.
- Verify `<forge-button>` projects `<ng-content>` correctly inside its template — a wrapping `<button>` with `<ng-content></ng-content>` should display the label. If text-variant styling sets `color: transparent` or the same color as the background, fix it.
- Add a contract test for `<forge-button variant="text">` and `<forge-button variant="outlined">` that asserts the projected label is visible (non-empty `textContent`, non-zero width, contrast ratio ≥ 3:1 against the card surface).
