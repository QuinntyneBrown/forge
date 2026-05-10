# HealthKit, Notifications, and Recovery States

Forge includes contracts and MVP stand-ins for HealthKit ingestion and reminder delivery, plus user-facing recovery screens for error and empty states.

## HealthKit Ingest Contract

Forge exposes a HealthKit ingest endpoint:

```http
POST /api/healthkit/ingest
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "sampleType": "activeEnergyBurned",
  "value": 250,
  "unit": "kcal",
  "recordedAt": "2026-05-10T12:00:00Z"
}
```

Validation:

- `sampleType` is required and limited to 64 characters.
- `unit` is required and limited to 16 characters.
- `value` must be 0 or greater.
- `recordedAt` is required by the request shape.

Current MVP behavior:

1. The endpoint requires authentication.
2. The request is validated.
3. The ingestion handler calls a deferred integration service.
4. The deferred service writes a structured log entry.
5. The current implementation does not yet create workout sessions or update dashboard calories from HealthKit samples.

The real Apple Watch or iOS HealthKit sync service can replace the logging stand-in later without changing the controller contract.

## Reminder Dispatcher

Forge runs a hosted notification dispatcher in the backend.

What it checks:

- Users with morning reminders enabled
- Users with kitchen nudges enabled
- Each user's configured time zone
- Each user's configured morning window start
- Each user's configured kitchen-closed start

How it runs:

1. The hosted service wakes up once per minute.
2. It checks whether a configured reminder boundary falls within the next 2 minutes.
3. It asks the notification sender to send the appropriate reminder.
4. The current notification sender logs the intended notification instead of delivering a real push, SMS, or email.

Logged reminder markers:

- Morning reminder: `notification.morning`
- Kitchen nudge: `notification.kitchen`

## Health Endpoints

Forge exposes two unauthenticated health endpoints.

Liveness:

```http
GET /health
```

Expected healthy response:

```json
{ "status": "Healthy" }
```

Readiness:

```http
GET /health/ready
```

Readiness checks database connectivity.

Healthy response:

```json
{ "status": "Healthy" }
```

Unhealthy response:

```json
{ "status": "Unhealthy" }
```

When the readiness check cannot connect to the database, the API returns HTTP 503.

## Error Page

The error page is at:

```text
/error
```

It can also include a trace id:

```text
/error?traceId=<trace-id>
```

The page shows:

- A user-facing error banner
- Backend health status
- HealthKit ingest status
- Trace id
- Go to dashboard action

Steps:

1. Open `/error?traceId=abc123`.
2. Confirm the trace id is visible.
3. Check backend health status.
4. Select Go to dashboard.

If you are signed out, Go to dashboard routes to `/dashboard`, and the auth guard redirects to `/sign-in`.

## Empty States

Forge uses explicit empty states for missing data.

Current examples:

- Fresh account on Workouts shows `No sessions yet`.
- Dashboard leaderboard shows `No one to compare with yet` when there are no comparable entries.
- Unknown route shows `Page not found`.

How to recover from the Workouts empty state:

1. Open Workouts.
2. Select Log your first session.
3. Fill the new-session form.
4. Save the session.

## Not-Found Page

If you open a route that does not exist:

1. Forge renders the not-found page.
2. The page says the route does not exist or has moved.
3. Select Go to dashboard to return to the authenticated home route.

If you are not signed in, the dashboard guard redirects to sign-in.

## Operational Logs

Forge uses structured JSON console logging. Request logs include:

- HTTP method
- Path
- Response status
- Duration
- Trace id
- User id when known

Sensitive values are redacted before logs reach providers. This includes password-like and token-like structured values.
