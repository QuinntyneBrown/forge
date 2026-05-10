# Bug 014: /workouts/:id edit page is missing the teal hero and the Duplicate / Delete actions

## Status
Open

## Severity
Medium

## Area
Workouts / Edit session

## References
- Implementation: http://localhost:4321/workouts/:id (existing session edit)
- Design mock: `file:///C:/projects/forge/docs/mocks/workout-detail.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/workout-detail.png`

## Description
Bugs 005 and 008 cover the **new** workout page (`/workouts/new`). The edit form at `/workouts/:id` (when opening an existing session) is a *separate* implementation and is missing two large pieces from the same mock: the teal gradient hero strip at the top, and the Actions card with Duplicate + Delete buttons (only Save changes + a single small base-points block remain).

## Expected Behavior
Per `docs/mocks/workout-detail.html` (the page is the same template — it represents both create and edit):
- **Teal hero** at the top of the page with eyebrow ("Treadmill · Tuesday, May 12"), title ("Easy zone 2 — 22 min"), sub ("5:12 AM · Synced from Apple Watch · Morning bonus applied"), and a 3-up "hero stats" row (Calories / Distance / Avg HR).
- **Points breakdown** card showing each line item (Base, Morning bonus, Zone 2 consistency, Streak multiplier) and a teal "Total earned" pill — not just a single "Base · 29 min logged · +58" row.
- **Actions** card containing: filled "Save changes" (teal), outlined "Duplicate", text "Delete session" (error red), plus an info caption "Deleting refunds points and removes from today's totals."

## Actual Behavior
- No hero — the page jumps straight from the top bar to the form card with the title "Edit session" rendered as a plain h1.
- Save button is rendered in blue (see Bug 009).
- "Points" block exists but only shows base; no morning bonus / streak multiplier / total pill rows.
- No Actions card at all — there is no Duplicate, no Delete, and no info caption.
- A stray empty rounded outline appears at the bottom of the page (visible in all three screenshots) — looks like an unstyled icon-button placeholder.

## Proposed Fix
- Reuse the same hero / points breakdown / actions layout that the new-workout page now uses, populating from the loaded session.
- Add Duplicate and Delete handlers (Duplicate → POST a copy; Delete → confirm dialog → DELETE, refund points, navigate back to list).
- Remove or style the stray empty outlined element near the bottom of the page (likely a misplaced button host).
