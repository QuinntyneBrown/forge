# Profile, Goals, and Behavioral Windows

The Profile page manages identity and daily target fields. Additional goal and behavioral settings are supported by the API and included in the current-user model, even where the web form does not yet expose controls.

## Open Profile

1. Sign in.
2. Select Profile from the navigation.
3. Review the Profile and settings page.

The form loads current values from `GET /api/me`.

## Edit Basic Profile Fields

The web form lets you edit:

- First name
- Last name
- Email
- Units
- Time zone
- Daily active calorie target
- Daily workout minutes target

Steps:

1. Open Profile.
2. Change one or more fields.
3. Select Save changes.
4. Wait for the Saved message.
5. Refresh the page or return later to confirm the new values persist.

If you change email, your next sign-in uses the new email address.

## Units

The current units choices are:

- Imperial (`lb / mi`)
- Metric (`kg / km`)

The profile setting is saved with the account. Existing workout forms currently display distance in miles and weight goal cards in pounds.

## Time Zone

Use an IANA time zone identifier, such as:

```text
America/Toronto
```

The time zone affects:

- Which sessions count as today on Dashboard
- Streak date boundaries
- Morning bonus scoring
- Reminder dispatch timing
- Kitchen-closed nudge timing

## Daily Targets

The profile form exposes:

- Daily active calorie target
- Daily workout minutes target

Defaults:

- `1500` active kcal per day
- `60` workout minutes per day

Validation:

- Daily active calorie target must be 100 to 10000.
- Daily workout minutes target must be 0 to 480.

The Dashboard ring and minutes line use these values.

## Current Weight Entries

Current weight tracking is supported by the API.

What it does:

1. Appends a historical weight entry.
2. Stores the weight in pounds.
3. Stores the current UTC timestamp.
4. Preserves prior entries.
5. Lets Dashboard compute month-to-date weight loss.

Endpoint:

```http
POST /api/profile/weight
Authorization: Bearer <access-token>
Content-Type: application/json

{ "weightLb": 220.5 }
```

Validation:

- Weight must be greater than 0.
- Weight must be less than or equal to 1500 lb.

Dashboard uses the first and latest weight entries in the current month to calculate month-to-date loss.

## Monthly Weight Goal

The monthly weight-loss goal is supported by the API and shown on Dashboard.

Default:

```text
20 lb per month
```

Endpoint:

```http
PUT /api/profile/weight-goal
Authorization: Bearer <access-token>
Content-Type: application/json

{ "monthlyWeightGoalLb": 20 }
```

Validation:

- Goal must be 1 to 30 lb per month.

## Morning Workout Window

The morning workout window controls:

- Morning bonus scoring
- Morning reminder scheduling

Default:

```text
05:00 to 07:30
```

Endpoint:

```http
PUT /api/profile/morning-window
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "start": "05:00:00",
  "end": "07:30:00",
  "reminderEnabled": true
}
```

Validation:

- Start must be earlier than end.

How it affects scoring:

1. Log a workout whose start time is inside the window.
2. Save the session.
3. Forge adds the morning bonus to the points ledger.

## Kitchen-Closed Window

The kitchen-closed window is a behavioral guardrail for late-night eating reminders.

Default:

```text
20:00 to 06:00
```

Endpoint:

```http
PUT /api/profile/kitchen-window
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "start": "20:00:00",
  "end": "06:00:00",
  "nudgeEnabled": true
}
```

Validation:

- Start and end must differ.

The current MVP notification sender logs the intended notification instead of delivering a real push notification.

## Leaderboard Opt-In

Leaderboard opt-in controls whether other users can see your row on their leaderboard.

Endpoint:

```http
PUT /api/profile/leaderboard-opt-in
Authorization: Bearer <access-token>
Content-Type: application/json

{ "leaderboardOptIn": true }
```

Rules:

- If opted out, other users do not see you.
- If opted in, other users can see your name and points.
- Your own leaderboard response can include your own row.

## Current User Fields

`GET /api/me` returns the full current-user profile:

- `id`
- `email`
- `firstName`
- `lastName`
- `role`
- `units`
- `timeZoneId`
- `dailyActiveCaloriesTarget`
- `dailyWorkoutMinutesTarget`
- `monthlyWeightGoalLb`
- `morningWindowStart`
- `morningWindowEnd`
- `kitchenClosedStart`
- `kitchenClosedEnd`
- `kitchenNudgeEnabled`
- `morningReminderEnabled`
- `leaderboardOptIn`

The web Profile form currently edits the identity, units, time zone, and daily target subset.
