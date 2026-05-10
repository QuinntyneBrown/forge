# API Reference for Advanced Use

This reference is for advanced users, testers, and client developers. Normal web-app flows are covered in the feature guides.

## Base URL

Local development API:

```text
https://localhost:5001
```

The helper scripts also poll the HTTP health endpoint on:

```text
http://localhost:5000/health
```

## Common Headers

Use JSON for request bodies:

```http
Content-Type: application/json
```

For authenticated endpoints, send:

```http
Authorization: Bearer <access-token>
```

## Enum Values

Equipment values:

- `Treadmill`
- `IndoorBike`
- `BenchPress`
- `Elliptical`

Session range values:

- `All`
- `Today`
- `Week`
- `Month`

Profile units:

- `Imperial`
- `Metric`

## Authentication Endpoints

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | No | Create account and start a session |
| `POST` | `/api/auth/sign-in` | No | Sign in and receive tokens |
| `POST` | `/api/auth/refresh` | No | Rotate refresh token and receive a new access token |
| `POST` | `/api/auth/sign-out` | Yes | Revoke the presented refresh-token family |
| `POST` | `/api/auth/password-reset/request` | No | Request a reset token |
| `POST` | `/api/auth/password-reset/confirm` | No | Set a new password with a valid reset token |

Register request:

```json
{
  "email": "user@example.com",
  "firstName": "First",
  "lastName": "Last",
  "password": "ForgeFit!2026"
}
```

Sign-in request:

```json
{
  "email": "user@example.com",
  "password": "ForgeFit!2026"
}
```

Auth response:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-token>",
  "userId": "<guid>",
  "email": "user@example.com",
  "role": "User"
}
```

## Current User and Profile Endpoints

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/me` | Yes | Get current user profile and settings |
| `DELETE` | `/api/me` | Yes | Delete current account |
| `PUT` | `/api/profile` | Yes | Update basic profile fields |
| `POST` | `/api/profile/weight` | Yes | Record current weight |
| `PUT` | `/api/profile/weight-goal` | Yes | Set monthly weight-loss goal |
| `PUT` | `/api/profile/morning-window` | Yes | Set morning window and reminder flag |
| `PUT` | `/api/profile/kitchen-window` | Yes | Set kitchen-closed window and nudge flag |
| `PUT` | `/api/profile/leaderboard-opt-in` | Yes | Set leaderboard visibility |

Update profile request:

```json
{
  "email": "user@example.com",
  "firstName": "First",
  "lastName": "Last",
  "units": "Imperial",
  "timeZoneId": "America/Toronto",
  "dailyActiveCaloriesTarget": 1500,
  "dailyWorkoutMinutesTarget": 60
}
```

## Dashboard Endpoint

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/dashboard` | Yes | Get summary values for Dashboard cards |

Dashboard response fields:

- `caloriesToday`
- `targetCalories`
- `minutesToday`
- `targetMinutes`
- `currentStreak`
- `currentBalance`
- `lifetimePoints`
- `tier`
- `nextRewardWithinReach`
- `monthToDateWeightLossLb`
- `monthlyWeightGoalLb`

## Session Endpoints

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/sessions` | Yes | List sessions |
| `POST` | `/api/sessions` | Yes | Create a session |
| `GET` | `/api/sessions/{id}` | Yes | Get one session |
| `PUT` | `/api/sessions/{id}` | Yes | Update a session |
| `POST` | `/api/sessions/{id}/duplicate` | Yes | Duplicate a session |
| `DELETE` | `/api/sessions/{id}` | Yes | Delete a session and refund points |

List query parameters:

- `equipment`: optional equipment enum value
- `range`: `All`, `Today`, `Week`, or `Month`
- `search`: optional notes substring
- `page`: page number
- `pageSize`: page size

Create or update request:

```json
{
  "equipment": "Treadmill",
  "startedAt": "2026-05-10T09:00:00-04:00",
  "durationMinutes": 22,
  "distanceMiles": 2.1,
  "avgHeartRateBpm": 128,
  "activeCalories": 218,
  "notes": "Steady morning run"
}
```

Session response:

```json
{
  "id": "<guid>",
  "equipment": "Treadmill",
  "startedAt": "2026-05-10T09:00:00-04:00",
  "durationMinutes": 22,
  "distanceMiles": 2.1,
  "avgHeartRateBpm": 128,
  "activeCalories": 218,
  "notes": "Steady morning run",
  "createdAt": "2026-05-10T13:00:00Z"
}
```

## Equipment Endpoint

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/equipment` | No | List supported equipment |

Response:

```json
[
  { "id": "Treadmill", "name": "Treadmill" },
  { "id": "IndoorBike", "name": "Indoor Bike" },
  { "id": "BenchPress", "name": "Bench Press" },
  { "id": "Elliptical", "name": "Elliptical" }
]
```

## Rewards and Tier Endpoints

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/rewards` | Yes | List active rewards |
| `POST` | `/api/rewards/{id}/redeem` | Yes | Redeem a reward |
| `GET` | `/api/tier` | Yes | Get current tier information |

Redeem response:

```json
{
  "redemptionId": "<guid>",
  "remainingBalance": 750
}
```

Tier response:

```json
{
  "name": "Iron",
  "lifetimePoints": 0,
  "nextTierName": "Bronze",
  "pointsToNextTier": 1000
}
```

## Leaderboard Endpoint

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/leaderboard` | Yes | List ranked leaderboard entries |

Query parameters:

- `page`: default `1`
- `pageSize`: default `25`, maximum `100`

Response row:

```json
{
  "userId": "<guid>",
  "firstName": "First",
  "lastName": "Last",
  "points": 1250,
  "rank": 1
}
```

## HealthKit Endpoint

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/healthkit/ingest` | Yes | Accept a HealthKit sample for deferred ingest |

Request:

```json
{
  "sampleType": "activeEnergyBurned",
  "value": 250,
  "unit": "kcal",
  "recordedAt": "2026-05-10T12:00:00Z"
}
```

Current behavior is logging-only deferred ingest.

## Health Endpoints

| Method | Path | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/health` | No | Liveness |
| `GET` | `/health/ready` | No | Database readiness |

## Common Response Codes

| Code | Meaning |
| --- | --- |
| `200` | Request succeeded and returned data |
| `201` | Session created |
| `202` | Password reset request or HealthKit ingest accepted |
| `204` | Request succeeded with no response body |
| `400` | Validation problem, invalid reset token, or insufficient points |
| `401` | Missing, invalid, or expired credentials |
| `404` | Session or reward was not found |
| `409` | Email already registered |
| `429` | Too many failed sign-in attempts |
| `500` | Unhandled server error |

Validation errors use `ValidationProblemDetails` with field-specific error arrays. Other handled errors use `ProblemDetails`.
