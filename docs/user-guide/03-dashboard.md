# Dashboard

The dashboard is the authenticated home screen at `/dashboard`. It summarizes your daily activity, workout streak, weight progress, points balance, tier, and leaderboard.

## Open the Dashboard

1. Sign in.
2. Select Home from the navigation.
3. Confirm the page title is Dashboard.

The top of the page shows the signed-in email and role. The Sign out button is also on this page.

## Daily Calories and Minutes

The daily ring card shows:

- Active calories logged today
- Daily active calorie target
- Workout minutes logged today
- Daily workout minute target

Defaults for a new account:

- Daily active calorie target: `1500` kcal
- Daily workout minute target: `60` minutes

How to use it:

1. Open Dashboard.
2. Find the ring card.
3. Read the large calorie number in the center of the ring.
4. Compare it against the target shown as `/ <target> kcal`.
5. Read the minutes line below the ring.

Calories and minutes come from workout sessions dated today in your configured time zone.

## Workout Streak

The streak card shows:

- Consecutive workout days ending today
- Current streak multiplier label

How to use it:

1. Open Dashboard.
2. Find the day streak card.
3. Use the day count to see how many consecutive local dates have at least one workout session.
4. Use the multiplier badge to understand the current streak bonus.

The displayed multiplier starts at `x1.00` and increases by `0.01` for each streak day, capped at `x1.50`.

## Weight Progress

The weight progress card shows:

- Monthly weight-loss goal
- Whether month-to-date progress is on track
- Month-to-date weight loss in pounds

Defaults for a new account:

- Monthly weight goal: `20` lb per month

How to use it:

1. Open Dashboard.
2. Find the weight progress card.
3. Read the goal line.
4. Read the status line.
5. Read the month-to-date line.

Month-to-date loss is calculated from weight entries recorded this month. If there are fewer than two entries this month, the value is `0.0 lb so far`.

Current weight entry and monthly weight-goal updates are API-backed features. See [Profile, Goals, and Behavioral Windows](06-profile-goals-settings.md).

## Tier and Points Balance

The tier card shows:

- Current tier name
- Current spendable points balance
- Points needed for the next tier, or a top-tier message

How to use it:

1. Open Dashboard.
2. Find the tier card.
3. Read the tier name.
4. Read the available point balance.
5. Read how many lifetime points remain before the next tier.

Spendable balance can go down when you redeem rewards or delete/refund sessions. Lifetime points count positive earned points for tier progression.

## Leaderboard

The leaderboard card shows up to 5 ranked entries.

How to use it:

1. Open Dashboard.
2. Find the Leaderboard card.
3. Read each row's rank, name, and points.

If no comparable users are available, the card says there is no one to compare with yet.

Leaderboard visibility is controlled by the leaderboard opt-in setting. The API includes the caller's own row even when the caller has not opted in, but other users appear only when they have opted in. See [Gamification, Rewards, and Leaderboard](05-gamification-rewards-leaderboard.md).

## Refresh Dashboard Values

Dashboard cards load their data when the page is opened.

To update the values after changing data:

1. Complete the action, such as saving a workout or redeeming a reward.
2. Return to `/dashboard`.
3. If the browser already has Dashboard open, refresh the page or navigate away and back.

Several actions route back to Dashboard automatically, including creating a new workout and deleting a workout.

## Empty Account Defaults

For a newly registered account with no sessions and no weight entries, expect:

- Calories today: `0 / 1500 kcal`
- Minutes today: `0 / 60 min today`
- Streak: `0 day streak`
- Streak multiplier: `x1.00`
- Weight progress: `20 lb / month`, `0.0 lb so far`
- Tier: `Iron`
- Points available: `0 pts`

## Dashboard Data Sources

| Dashboard value | Source |
| --- | --- |
| Calories today | Sum of today's workout session active calories |
| Minutes today | Sum of today's workout session duration minutes |
| Daily targets | Profile fields |
| Streak | Distinct workout dates in user's time zone |
| Current balance | Sum of all points ledger entries |
| Lifetime points | Sum of positive points ledger entries |
| Tier | Lifetime points threshold |
| Next reward | Lowest active reward with cost greater than or equal to balance |
| Month-to-date loss | First and latest weight entries in current month |
