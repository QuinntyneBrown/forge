# Bug 018: Dashboard cards lack the rich content, color treatment, and structure of the mock

## Status
Open

## Severity
High

## Area
Dashboard

## References
- Implementation: http://localhost:4321/dashboard
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html`
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/dashboard.png`

## Description
Bug 004 was closed as "five cards exist, deeper polish deferred". Side-by-side review against the mock shows the cards diverge enough from the design that it deserves its own follow-up. Specifically the hero card is not the dark teal gradient hero, the Streak/Rewards card is missing its sub-elements, the Eating window card and Today's Sessions card are not present at all, and no card has the iconography or eyebrow labels the mock specifies.

This is a checklist bug — group the deviations rather than file 5 small ones.

## Expected Behavior — checklist
- [ ] **Hero "Today's active calories" card** uses the dark teal-to-emerald linear gradient (`linear-gradient(135deg, #0E5A4D 0%, #106B5C 60%, #1B7A6A 100%)`) with white text. Currently it is a light-tinted card with the calorie ring drawn in blue and no eyebrow.
- [ ] Hero card includes the eyebrow `TODAY'S ACTIVE CALORIES`, a sub-headline `You're 320 cal from your daily goal`, and a 3-row "stat-list" beside the ring (Workout minutes / Avg heart rate / Weight trend) — currently only the ring + a duration "94 / 60 min today" appear.
- [ ] Hero card includes two CTAs: peach-filled "Start morning workout" and a tonal "View today's sessions" — neither is present.
- [ ] **Streak & Rewards card** renders title "7-day morning streak" + sub + a 2-up mini-stat grid (Reward points today + sparkline, Total points balance + Redeem link) + a horizontally scrolling badge row (Morning Warrior / 1500-Cal Club / Night Resister). Currently only "4 / day streak / ×1.04" is shown, no badges, no points stats, no sparkline.
- [ ] **Eating window card** is entirely missing. Should include an icon tile, "Fasting until 6:00 AM" title, sub, an "On track" chip, a horizontal progress bar with marker, and a 3-tick legend.
- [ ] **Today's sessions card** is entirely missing. Should list 2–3 logged sessions for today (Treadmill, Bike, etc.) with icon tile, title, meta line, and `+pts`.
- [ ] **Leaderboard card** present in implementation does not exist in the mock for the dashboard at all — confirm whether it should stay (and where) or be moved to Rewards.

## Actual Behavior
- 4 cards rendered: a light calorie-ring card, a small streak card, a "-20 lb / month · Behind" card (not in mock at this position), a Tier ("Gold / 17965 pts") card (mock shows tier on Rewards, not Dashboard), and a Leaderboard card.
- No eating-window card, no today's-sessions card, no badges row, no sparkline.
- All accent colors are blue instead of the mock's teal / peach / amber.

## Proposed Fix
- Restructure `DashboardPageComponent` to render the 4 mock cards (hero / streak+rewards / eating-window / today's sessions) in a 12-col grid that collapses to 1-col on mobile, per the mock's media-query breakpoints (1100px → 12-col, 768px → 2-col, default → 1-col).
- Build `HeroCalorieRingCardComponent`, `StreakRewardsCardComponent`, `EatingWindowCardComponent`, `TodaysSessionsCardComponent`.
- Decide whether the Leaderboard belongs on the dashboard at all; if yes, agree on a placement and add it back as a 5th card with proper styling.
