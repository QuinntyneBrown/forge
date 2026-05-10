# Bug 024: Workout-detail Actions card renders an empty circle instead of Duplicate / Delete buttons

## Status
Complete

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

## Resolution

### Root cause
The `<forge-button>` template (`frontend/projects/components/src/lib/button/button.component.html`) used a separate `<ng-content></ng-content>` inside each `@switch` branch (one for `outlined`, `text`, and `filled`). Angular content projection happens once when the component is created and the projected nodes get attached to the FIRST `<ng-content>` slot in declaration order — so only the `outlined` branch (the first one) ever received the projected label. When the active variant was `text` (`Delete session`) the projected text was nowhere on the page; when it was `outlined` (`Duplicate`) the projection had been logically attached but Material's MDC button still rendered the label slot empty because the same physical projected nodes can be claimed by only one consumer at runtime, and on the workout-detail page that consumer was the OTHER button instance further up the DOM. Net effect: outlined Duplicate showed an empty oval, text Delete showed nothing at all.

The bug was latent in any view rendering an `outlined` or `text` `<forge-button>`. The dev seed only routed users through the `filled` default until Bug 014 introduced the workout-detail Actions card, which is why the regression surfaced there.

### Fix
- `frontend/projects/components/src/lib/button/button.component.html` — declare the projected label exactly once in an `<ng-template #label><ng-content></ng-content></ng-template>` and stamp it inside each `@switch` branch via `<ng-container *ngTemplateOutlet="label"></ng-container>`. Single content-projection slot, rendered into whichever branch is active.
- `frontend/projects/components/src/lib/button/button.component.ts` — added `NgTemplateOutlet` to `imports`.

### Tests
- `frontend/e2e/tests/workout-detail-actions-visible.spec.ts` — new spec sign-in, navigates to `/workouts`, opens the first row, asserts both `workout-detail-duplicate` and `workout-detail-delete` are visible, contain their labels in `textContent`, and have a resolved color distinct from their resolved background (catches white-on-white).
- Smoke regression: `workout-create.spec.ts`, `workout-delete-refund.spec.ts`, `workouts-list.spec.ts`, `workouts-list-content-and-styling.spec.ts` all green.

### Positive externalities
The same fix restores correct label projection for every `<forge-button variant="outlined">` and `<forge-button variant="text">` site-wide (rewards-catalog, password-reset, profile-form, workout-detail, etc.) — anywhere a non-default variant rendered with an empty label is now fixed.
