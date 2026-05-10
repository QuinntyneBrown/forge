# Bug 025: Profile page cleanup — duplicate Save, stranded Danger zone, missing Apple Watch and Theme rows

## Status
Open

## Severity
Medium

## Area
Profile / settings

## References
- Implementation: http://localhost:4321/profile
- Design mock: `file:///C:/projects/forge/docs/mocks/profile.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/profile.png`
- Prior bug: `docs/bugs/011-profile-missing-hero-and-multiple-sections.md` (closed; explicitly deferred Apple Watch row, Theme row, and Save-button consolidation)

## Description
Bug 011 closed the Profile rebuild but flagged three follow-ups for a later pass: (1) consolidate the two `Save changes` buttons, (2) add the Apple Watch sync row to Integrations, (3) add the Theme row. The second-pass review confirms all three are still outstanding, plus a fourth: the original "Danger zone" eyebrow + paragraph is still rendered inside the Account card with no destructive action button — leftover stub copy from the pre-rebuild page.

This is a checklist bug.

## Expected Behavior — checklist

- [ ] Only one `Save changes` button on the page (the bottom Save card). The duplicate inside the Account card should be removed; the Account card should be a pure "edit your identity" surface like Goals / Windows above the Save card.
- [ ] The "DANGER ZONE" eyebrow + the paragraph "Deleting your account permanently removes …" should either be removed entirely (the mock has no danger zone on profile) or moved into the Save card with a real `Delete account` outlined-error button. The current state — copy with no action — is the worst of both.
- [ ] **Apple Watch sync row** added to the Integrations & alerts card as the first row: black/dark icon tile with `watch` glyph, title "Apple Watch sync", sub "Pull workouts and heart-rate from HealthKit", with a switch.
- [ ] **Theme row** added to the Integrations & alerts card with a colored icon tile (`palette` glyph), title "Theme", sub "System (auto)", and a `Change` text-link instead of a switch (per the mock pattern).

## Actual Behavior
- TWO Save changes buttons render — one inside the Account card under the Daily-active-calories field, one inside the bottom Save card.
- The Account card still shows "DANGER ZONE" + the deletion paragraph with no button — pure dead text.
- Integrations card shows only Morning workout reminder, Kitchen-closes nudge, Friend leaderboard. No Apple Watch row, no Theme row.

## Proposed Fix
- Remove the in-card Save button from the Account card template; the Save card already owns the save action and fires `forkJoin(...)` of all five `PUT` endpoints.
- Delete the Danger-zone block from the Account card template, OR add a `Delete account` outlined-error button inside the Save card (below Sign out) wired to a confirm dialog. Pick one and do it; do not leave the orphan copy.
- Add `appleWatchSyncEnabled` and a `theme` preference to the profile signals + bind them via `<button role="switch">` and `<a class="…">Change</a>` respectively. Stub the persistence as a no-op until a backend endpoint exists, but show the rows.
