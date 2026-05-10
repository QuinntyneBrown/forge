# Workouts

Forge Fit lets you log workout sessions for the supported home equipment, review past sessions, filter the list, edit details, duplicate a session, and delete a session.

## Supported Equipment

Forge supports exactly four equipment types:

- Treadmill
- Indoor Bike
- Bench Press
- Elliptical

These equipment values are used in the workout list filters, the new-session form, the edit form, and the API.

## Open the Workout List

1. Sign in.
2. Select Workouts from the navigation.
3. Review the Workouts page.

If you have no sessions yet, Forge shows an empty state with a Log your first session action.

## Filter Workout Sessions

The Workouts page has two filter rows.

Equipment filters:

1. Select All equipment to show every session.
2. Select Treadmill, Indoor Bike, Bench Press, or Elliptical to show one equipment type.

Date range filters:

1. Select All to show all sessions.
2. Select Today to show sessions from today.
3. Select This week to show recent sessions from the last 7 days.
4. Select This month to show recent sessions from the last 30 days.

The list displays sessions in reverse chronological order. Each row shows equipment, duration, active calories, and notes when notes exist.

The backend also supports notes search through the `search` query parameter on `GET /api/sessions`. The current web list does not expose a search input.

## Create a Workout Session

1. Sign in.
2. Open Workouts.
3. Select New session.
4. Choose Equipment.
5. Choose Date.
6. Choose Start time.
7. Enter Duration in minutes.
8. If the equipment is not Bench Press, enter optional Distance in miles.
9. Enter optional Avg HR in beats per minute.
10. Enter Active calories.
11. Enter optional Notes.
12. Select Save session.

After saving, Forge routes to Dashboard. The new session contributes to:

- Today's active calories, if the session is today
- Today's workout minutes, if the session is today
- Current streak
- Points balance
- Tier progress
- Rewards affordability

## Bench Press and Distance

Distance does not apply to Bench Press sessions.

When you choose Bench Press:

1. The distance field is hidden.
2. The submitted distance is set to `null`.
3. Distance is not required.

## Session Field Rules

| Field | Rule |
| --- | --- |
| Equipment | Must be one of the four supported equipment values |
| Started at | Required date and time |
| Duration | 1 to 480 minutes |
| Distance | Optional, must be 0 or greater |
| Average heart rate | Optional, 30 to 240 bpm |
| Active calories | 0 to 5000 kcal |
| Notes | Optional, up to 2000 characters |

Invalid requests return validation errors from the API. The web form disables submit while obvious client-side validation fails.

## Open a Workout Session

1. Open Workouts.
2. Find the session row.
3. Select the row.

Forge opens `/workouts/:id` and loads the edit form plus a points breakdown card.

## Edit a Workout Session

1. Open a session from the workout list.
2. Change any field.
3. Select Save changes.

After saving, Forge returns to the Workouts page.

If equipment, started-at time, or duration changes, Forge refunds the prior points for that session and scores it again. This keeps the points ledger accurate for material workout changes.

## Duplicate a Workout Session

1. Open a session from the workout list.
2. Select Duplicate.

Forge creates a new session with:

- The same equipment
- The same duration
- The same distance
- The same average heart rate
- The same active calories
- The same notes
- A new start time set to the current time

After duplication, Forge returns to the Workouts page.

## Delete a Workout Session

1. Open a session from the workout list.
2. Select Delete session.

Forge deletes the workout and routes to Dashboard.

Before removing the session row, Forge writes a compensating refund in the points ledger. Your points balance decreases by the points previously awarded for that session.

## Points Breakdown on Session Detail

The session detail page shows a base-points breakdown:

- Base points are `2` points per workout minute.
- A 22-minute workout shows a base subtotal of `44` points.

Morning bonus and streak multiplier points are applied by the backend and reflected in Dashboard balance and tier progress.

See [Gamification, Rewards, and Leaderboard](05-gamification-rewards-leaderboard.md) for the full scoring rules.

## Workout List API Paging

The API returns a paged result:

- `items`: sessions for the current page
- `page`: page number
- `pageSize`: page size
- `total`: total matching sessions

The web app currently requests page `1` with page size `50`.
