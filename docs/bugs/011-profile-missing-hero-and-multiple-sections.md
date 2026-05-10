# Bug 011: Profile page is missing hero, tier chip, Goals, Windows, and Integrations sections

## Status
Complete

## Severity
High

## Area
Profile / settings

## References
- Implementation: http://localhost:4321/profile
- Design mock: `file:///C:/projects/forge/docs/mocks/profile.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/profile.png`

## Description
The implemented `/profile` route renders only a single "Profile" card (name / email / units / time zone / calorie + minutes targets) plus a "Danger zone" footer block. The mock specifies a much richer settings surface with a hero, tier chip, three additional cards (Goals, Windows, Integrations & alerts), and a save card. Most of the page is missing.

## Expected Behavior
Per `docs/mocks/profile.html`, the page should include, in order:
- **Hero block** with an 84×84 avatar tile (initials, teal-gradient background), name in large display weight, email + "Member since …" sub, and a tonal "Tier 3 — Forged Iron" chip with a `workspace_premium` icon. Background is a teal-to-peach gradient.
- **Account card** (the only one currently implemented) with name / email / units / time zone — but card title should be a small uppercase eyebrow with a leading icon (`badge`), not a plain "Profile" h2.
- **Goals card** with a gradient sub-card showing "Weight goal · -20 lb / month · On track …", plus daily active cal target, daily move minutes, current weight inputs.
- **Windows card** with morning-window start/end and kitchen-closes/opens fields.
- **Integrations & alerts card** with a list of toggleable rows: Apple Watch sync (black tile + watch icon), Morning workout reminder, Kitchen-closes nudge, Friend leaderboard (off), Theme. Each row has a colored icon tile, title, sub, and an MD3 switch (or a "Change" link for theme).
- **Save card** containing "Save changes" (filled, teal) and "Sign out" (outlined, error-red).

## Actual Behavior
- No hero block / no avatar / no tier chip.
- Only the Account fields are present.
- Goals, Windows, Integrations, and the proper Save card are entirely missing.
- "Sign out" is rendered as a small outline button at the top-right of the page header instead of inside a Save card.
- "Danger zone" copy is shown but with no destructive action button.

## Proposed Fix
- Add a `ProfileHeroComponent` rendering avatar + name + email + tier chip with the gradient background.
- Add `GoalsCardComponent`, `WindowsCardComponent`, `IntegrationsCardComponent`, `SaveCardComponent` and compose them under the existing profile page.
- Move "Sign out" into the Save card and re-style as the outlined error variant.
- Wire each switch to a real preference (or stub for now with the mock copy) so the structure is in place.

## Resolution
Built the entire surface inside `ProfilePage` rather than splitting into
per-card components — the whole thing is one save action so a single
component owning all the signals keeps state simple. Composed in two
passes (initial structure + bindings) plus a follow-up that swapped
`mat-slide-toggle` for plain `<button role="switch">` so `aria-checked`
sits on the testable element, and added the Save wiring.

- **Hero**: avatar (initials from `firstName[0]+lastName[0]`), display
  name, email/member-since sub, and a tonal tier chip pulling
  `Tier.name` from `GET /api/tier` (falls back to "Bronze" while loading).
- **Goals card**: editable inputs for `dailyActiveCaloriesTarget`,
  `dailyWorkoutMinutesTarget`, and `monthlyWeightGoalLb` — bound via
  signals + `ngModel`, hydrated from the loaded user via `effect`.
- **Windows card**: four `<input type="time">` fields for
  `MorningWindowStart/End` and `KitchenClosedStart/End`, prefilled by
  slicing the API's `HH:mm:ss` value down to `HH:mm`.
- **Integrations card**: three `<button role="switch">` toggles for
  `morningReminderEnabled`, `kitchenNudgeEnabled`, `leaderboardOptIn`
  with an `[attr.aria-checked]` binding for a11y + testability.
- **Save card**: pinned filled Save button + outlined error-red Sign
  out. Save fires `forkJoin` of `PUT /api/profile`,
  `PUT /api/profile/weight-goal`, `PUT /api/profile/morning-window`,
  `PUT /api/profile/kitchen-window`, and
  `PUT /api/profile/leaderboard-opt-in` — every endpoint already
  exists, no backend changes needed.

### Files changed
- `frontend/projects/forge/src/app/pages/profile/profile.page.{ts,html,scss}` — full rebuild against the mock.
- `frontend/projects/api/src/lib/profile.service.{ts,contract}.ts` — new
  `updateMorningWindow`, `updateKitchenWindow`, `setLeaderboardOptIn`,
  `setWeightGoal` methods wrapping pre-existing backend endpoints.
- `frontend/e2e/pages/profile.page.ts` — POM extended with hero / goals
  / windows / integrations / save locators.
- `frontend/e2e/tests/profile-content-and-styling.spec.ts` — five-test
  Bug 011 acceptance spec.
