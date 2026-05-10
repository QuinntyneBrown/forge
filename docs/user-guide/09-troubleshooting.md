# Troubleshooting

Use this page when a common Forge Fit flow does not behave as expected.

## I Cannot Reach the App

1. Confirm the backend is running.
2. Open `http://localhost:5000/health`.
3. Confirm it returns `{ "status": "Healthy" }`.
4. Confirm the frontend is running.
5. If you used `scripts/start-local.ps1` or `scripts/start-local.sh`, open `http://localhost:4321`.
6. If you used `npm start`, open `http://localhost:4200`.

The Angular app calls the API at `https://localhost:5001`.

## The API Starts But Database Calls Fail

1. Check `backend/src/Forge.Api/appsettings.json`.
2. Confirm `ConnectionStrings:DefaultConnection` points to a reachable SQL Server.
3. Confirm SQL Server is running.
4. Restart the API.

The API applies migrations during startup. If migration fails, the API process logs the database error.

## Browser Blocks the Local API Certificate

Local HTTPS uses a development certificate.

1. Trust the .NET development certificate:

   ```bash
   dotnet dev-certs https --trust
   ```

2. Restart the backend.
3. Reload the frontend.

## Sign-In Fails

Check these items:

1. Email is correct.
2. Password is correct.
3. Account was not deleted.
4. You have not hit the failed sign-in lockout.
5. The backend is reachable.

In Development, the seeded account is:

```text
dev@forge.local
DevPassword123!
```

After 5 failed attempts in 15 minutes, Forge temporarily locks sign-in for that email.

## Remember Me Did Not Persist

Remember me uses browser local storage.

Common causes:

- Remember me was not checked.
- Browser storage was cleared.
- Private browsing blocked or removed local storage.
- The refresh token expired or was revoked.
- A consumed refresh token was reused and the token family was revoked.

Sign in again to create a new session.

## Password Reset Email Does Not Arrive

The current MVP does not send real password reset email.

For local development:

1. Submit the reset request at `/password-reset`.
2. Look at the backend console logs.
3. Find the `password-reset.email.deferred` log entry.
4. Copy the token.
5. Open `/password-reset?token=<token>`.

The token expires after 30 minutes and can be used once.

## The Workouts Page Is Empty

This is expected for a fresh account.

1. Open Workouts.
2. Select Log your first session or New session.
3. Fill out the workout form.
4. Save the session.

After saving, Dashboard should show updated calories, minutes, points, and streak values.

## The Bench Press Form Has No Distance Field

This is expected. Bench Press sessions do not require distance, and Forge submits `null` for distance.

## My Points Balance Looks Lower Than Expected

Check whether one of these happened:

- You redeemed a reward.
- You deleted a workout session.
- You edited a material workout field and Forge refunded and rescored it.
- The workout was outside the morning bonus window.
- Your streak reset after a missed day.

Current balance is spendable points after refunds and redemptions. Lifetime points are used for tier progression.

## A Reward Cannot Be Redeemed

The Redeem button is disabled when your balance is below the reward cost.

To earn more points:

1. Log more workout minutes.
2. Log workouts inside the morning window for the bonus.
3. Maintain a daily streak for multiplier points.

## Dashboard Does Not Update Immediately

Dashboard cards load data when the page is opened.

To refresh:

1. Navigate away and back to Dashboard.
2. Or reload the browser page.

Creating and deleting workouts already route you back to Dashboard automatically.

## Leaderboard Shows No One to Compare With

Other users must opt in before they appear to you.

Your own row can appear in your own leaderboard response. The dashboard card requests the first 5 rows.

## HealthKit Samples Do Not Change Dashboard

This is expected in the current MVP.

The HealthKit endpoint validates and accepts samples, then logs the deferred ingest action. It does not yet create sessions or update active calories.

## Notifications Do Not Arrive

This is expected in the current MVP.

The notification dispatcher runs, but the sender is a logging stand-in. Check backend logs for:

- `notification.morning`
- `notification.kitchen`

## Error Page Shows Backend Unreachable

1. Confirm the API is running.
2. Open `http://localhost:5000/health`.
3. Open `http://localhost:5000/health/ready`.
4. Check SQL Server connectivity if readiness is unhealthy.
5. Use the trace id on the error page to find matching backend logs.

## Unknown Route Opens Page Not Found

This is expected.

1. Select Go to dashboard.
2. If signed out, sign in.
3. Use the primary navigation to open a known route.
