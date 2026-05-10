# Bug 022: Dashboard hero and streak/rewards cards have wrong copy and missing sub-content

## Status
Complete

## Severity
Medium

## Area
Dashboard

## References
- Implementation: http://localhost:4321/dashboard
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/dashboard.png`

## Description
Bug 018 closed the structural rebuild of the dashboard cards (hero gradient, eating window, today's sessions, badges, sparkline). A second-pass review against `dashboard.html` shows two cards still diverge from the mock in copy and in the sub-elements that flank the headline number.

This is a checklist bug for the two specific cards.

## Expected Behavior — checklist

- [ ] **Hero "Today's active calories" card title** reads `You're 320 cal from your daily goal` (or the correct dynamic equivalent: "You're {goal - actual} cal from your daily goal"). Implementation currently hard-codes `Keep the streak going`.
- [ ] **Hero card stat-list** beside the calorie ring renders three rows — Workout minutes / Avg heart rate / Weight trend — each with an icon tile, an uppercase eyebrow label, and a value (`52 min`, `142 bpm`, `214.8 lb · -3.2 / wk`). Implementation currently shows a single thin line `… / 60 min today` and nothing else.
- [ ] **Streak & rewards card title** reads `7-day morning streak` (or `{N}-day morning streak`) with sub `Best in the last 30 days. Keep going to hit Morning Warrior x10.` Implementation currently shows `4 / day streak / x1.04` as the entire body.
- [ ] **Streak card mini-stat grid** renders the 2-up grid: left tile is `Reward points today / +165 pts / sparkline`; right tile is `Total points balance / 2,840 pts / Redeem ›` link. Implementation currently shows only a single `Reward points · last 7 days` row with the sparkline and no point totals or Redeem link.

## Actual Behavior
- Hero card title is the streak-focused "Keep the streak going" phrase.
- Hero card right column shows just a tiny `60 min today` text — no icon tiles, no HR row, no weight trend row.
- Streak card body collapses to "4 / day streak / ×1.04" with no headline copy and no mini-stat grid.

## Proposed Fix
- Bind hero title to `(goal - active).toLocaleString() + ' cal from your daily goal'` so it matches the mock copy.
- Add the 3-row stat list (Workout minutes / Avg HR / Weight trend) — values can come from the existing `IMeService` / sessions today aggregation; HR/weight will need a stub if no endpoint exists yet, flagged as deferred follow-up similar to the badge placeholders.
- Restructure the streak card body to include the headline `${streak}-day morning streak` + sub copy + the 2-up `Reward points today` / `Total points balance` mini-stat grid with the existing sparkline scoped to the left tile.
- Move the `×1.04` multiplier out of the title position (mock places it implicitly in the rewards/multiplier flow, not as a tier badge above the streak number).
