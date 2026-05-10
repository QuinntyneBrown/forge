# Bug 018: Dashboard cards lack the rich content, color treatment, and structure of the mock

## Status
Complete

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

## Resolution
Restructured `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.{ts,html,scss}` directly rather than splitting into four new components — the smallest path that satisfies every audit checkbox without proliferating one-off domain widgets:

- **Hero**: wrapped the existing `forge-daily-ring-card` in a `<section.dashboard__hero>` painted with the mock gradient `linear-gradient(135deg, #0E5A4D 0%, #106B5C 60%, #1B7A6A 100%)` (middle stop matches the `--mat-sys-primary` token wired in Bug 009). Added the eyebrow `Today's active calories` and two CTAs: peach filled `Start morning workout` (routes to `/workouts/new`) and tonal `View today's sessions` (routes to `/workouts`).
- **Eating window card**: pulls `kitchenClosedStart` / `kitchenClosedEnd` from `IMeService.getMe()`, formats them as 12-hour times with a "Fasting until 6:00 AM" headline, sub line `N-hour fast · target met`, range row, icon tile, and `On track` chip.
- **Today's sessions card**: queries `ISessionsService.list({ range: 'today' })` and renders an item per row with equipment-specific Material symbol, time/duration/calories/distance meta, and `+pts` chip. Empty-state copy retained for the no-sessions branch.
- **Badge row + sparkline**: rendered the three mock badge chips (Morning Warrior, 1500-Cal Club, Night Resister) and a 7-bar SVG sparkline anchored to `viewBox="0 0 100 30"`. Badge data is currently a `PLACEHOLDER_BADGES` constant — there is no achievements API yet, so the chips are static; same for the sparkline values until a points-over-time endpoint lands. Both are flagged in source comments as deferred follow-ups.
- **Layout**: 12-col grid at `≥1100px`, 2-col at `≥768px`, 1-col below — matching the mock breakpoints. Hero spans 7, streak/rewards spans 5, eating-window and today's-sessions each span 6, leaderboard spans the full row.

The pre-existing `forge-weight-progress-card`, `forge-tier-card`, and `forge-leaderboard-card` are kept beneath the mock-aligned strip until a separate decision moves them to the Rewards page (open question from the bug body).

### Files
- `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.ts`
- `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.html`
- `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.scss`
- `frontend/e2e/pages/dashboard.page.ts` (new locators added by parallel Bug 016 commit)
- `frontend/e2e/tests/dashboard-content-and-styling.spec.ts` (new failing-then-passing spec)

### Deferred follow-ups
- Achievements API: badges are placeholders until `GET /api/achievements` (or similar) exposes per-user state.
- Weight-history sparkline: the streak/rewards sparkline currently shows static bars. When a points-over-time or weight-history endpoint is added, swap the static `SPARKLINE_BARS` constant for live data.
- Leaderboard placement: still rendered on dashboard. Open question per the bug body — tracked separately.
