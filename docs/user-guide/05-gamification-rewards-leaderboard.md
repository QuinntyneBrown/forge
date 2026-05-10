# Gamification, Rewards, and Leaderboard

Forge Fit turns workouts into points, streak progress, tier progress, and spendable rewards.

## Earn Base Points

Every saved workout earns base points:

```text
base points = duration minutes x 2
```

Examples:

| Duration | Base points |
| --- | --- |
| 10 minutes | 20 points |
| 22 minutes | 44 points |
| 60 minutes | 120 points |

Base points are written to the points ledger when a session is created, duplicated, or rescored after a material edit.

## Earn the Morning Bonus

Forge awards a morning bonus when a workout starts inside your configured morning window.

Default morning window:

```text
05:00 to 07:30
```

Morning bonus:

```text
+25 points
```

How to earn it:

1. Keep your profile time zone accurate.
2. Log a workout whose start time falls inside your morning window.
3. Save the session.
4. Check Dashboard for the updated points balance.

The morning window is API-backed. See [Profile, Goals, and Behavioral Windows](06-profile-goals-settings.md).

## Build a Streak

A workout streak is the count of consecutive local dates, ending today, that contain at least one workout session.

How to build one:

1. Log at least one workout today.
2. Log at least one workout tomorrow.
3. Continue logging one or more sessions each day.
4. Check Dashboard for the updated day streak.

Missing a day resets the streak on the next logged day.

## Streak Multiplier

The streak multiplier is:

```text
1.00 + (0.01 x streak days)
```

It is capped at:

```text
1.50
```

When the multiplier is greater than `1.00`, Forge adds a streak bonus based on base points.

Example:

1. A 7-day streak has multiplier `1.07`.
2. A 22-minute session earns `44` base points.
3. Streak bonus is based on `44 x 0.07`.
4. Forge floors that value to a whole number of bonus points.

## Current Balance vs Lifetime Points

Forge tracks two point concepts:

- Current balance: spendable points after earning, refunds, and redemptions.
- Lifetime points: positive earned points used for tier calculation.

Redeeming rewards lowers current balance but does not erase positive lifetime earning history.

Deleting or materially editing sessions writes refund rows so balance stays accurate.

## Tier Progression

Tiers are based on lifetime points.

| Tier | Minimum lifetime points |
| --- | ---: |
| Iron | 0 |
| Bronze | 1000 |
| Silver | 2500 |
| Forged Iron | 5000 |
| Gold | 10000 |
| Platinum | 25000 |

How to check your tier:

1. Open Dashboard.
2. Find the tier card.
3. Read the tier name.
4. Read the points needed for the next tier.

You can also open Rewards, where the tier card appears above the catalog.

## Rewards Catalog

Open `/rewards` to view the reward catalog and spend points.

Seeded rewards:

| Reward | Cost |
| --- | ---: |
| Post-workout Smoothie | 200 pts |
| Rest Day Pass | 500 pts |
| New Athletic Socks | 750 pts |
| Pair of Wireless Earbuds | 4000 pts |
| Premium Whey Protein | 6000 pts |
| Massage Session | 8000 pts |
| New Running Shoes | 12000 pts |

Each reward row shows:

- Reward name
- Description
- Cost
- Progress ring based on current point balance
- Redeem button

## Redeem a Reward

1. Sign in.
2. Open Rewards.
3. Read your available point balance.
4. Find a reward you can afford.
5. Select Redeem.
6. Confirm the balance decreases by the reward cost.

The Redeem button is disabled when the reward costs more than your current balance.

If a direct API call tries to redeem an unaffordable reward, the backend returns `INSUFFICIENT_POINTS` and leaves the balance unchanged.

## Leaderboard

The dashboard leaderboard card shows ranked users by current points.

Rows include:

- Rank
- First and last name
- Points

Rules:

- The current user can appear in their own leaderboard result.
- Other users appear only when they have opted in.
- Results are sorted by points descending, then last name, then first name.
- The dashboard requests the first 5 entries.

Leaderboard opt-in is an API-backed profile setting. See [Profile, Goals, and Behavioral Windows](06-profile-goals-settings.md).

## Practical Scoring Flow

To see the gamification loop end to end:

1. Open Workouts.
2. Select New session.
3. Log a 22-minute treadmill workout.
4. Set the start time inside your morning window if you want the morning bonus.
5. Save the session.
6. Open Dashboard.
7. Confirm calories, minutes, streak, points, and tier update.
8. Open Rewards.
9. Redeem an affordable reward.
10. Return to Dashboard and confirm the available balance changed.
