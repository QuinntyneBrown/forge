# Forge Fit User Guide

Forge Fit is a fitness gamification app for tracking home workouts, building a morning routine, watching daily calorie and minute targets, and spending earned points on personal rewards.

This guide covers the current Forge implementation in the repository. It includes both the web app features and the API-backed features that are available but not fully exposed as web controls yet.

## Guide Index

1. [Getting Started](01-getting-started.md)
2. [Account and Security](02-account-and-security.md)
3. [Dashboard](03-dashboard.md)
4. [Workouts](04-workouts.md)
5. [Gamification, Rewards, and Leaderboard](05-gamification-rewards-leaderboard.md)
6. [Profile, Goals, and Behavioral Windows](06-profile-goals-settings.md)
7. [HealthKit, Notifications, and Recovery States](07-integrations-notifications-recovery.md)
8. [API Reference for Advanced Use](08-api-reference.md)
9. [Troubleshooting](09-troubleshooting.md)

## Feature Map

| Feature area | Web app route or surface | API support | Guide |
| --- | --- | --- | --- |
| Sign up | `/sign-up` | `POST /api/auth/register` | [Account and Security](02-account-and-security.md) |
| Sign in | `/sign-in` | `POST /api/auth/sign-in` | [Account and Security](02-account-and-security.md) |
| Remember me and refresh | Sign-in form | `POST /api/auth/refresh` | [Account and Security](02-account-and-security.md) |
| Sign out | Dashboard button | `POST /api/auth/sign-out` | [Account and Security](02-account-and-security.md) |
| Password reset request | `/password-reset` | `POST /api/auth/password-reset/request` | [Account and Security](02-account-and-security.md) |
| Password reset confirmation | `/password-reset?token=...` | `POST /api/auth/password-reset/confirm` | [Account and Security](02-account-and-security.md) |
| Account deletion | `/profile` danger zone | `DELETE /api/me` | [Account and Security](02-account-and-security.md) |
| Dashboard summary | `/dashboard` | `GET /api/dashboard` | [Dashboard](03-dashboard.md) |
| Workout list and filters | `/workouts` | `GET /api/sessions` | [Workouts](04-workouts.md) |
| New workout session | `/workouts/new` | `POST /api/sessions` | [Workouts](04-workouts.md) |
| Edit, duplicate, delete session | `/workouts/:id` | session detail endpoints | [Workouts](04-workouts.md) |
| Supported equipment | Workout forms and filters | `GET /api/equipment` | [Workouts](04-workouts.md) |
| Points and streaks | Dashboard, workout detail, rewards | points ledger | [Gamification](05-gamification-rewards-leaderboard.md) |
| Rewards catalog and redemption | `/rewards` | `GET /api/rewards`, `POST /api/rewards/{id}/redeem` | [Gamification](05-gamification-rewards-leaderboard.md) |
| Tier progression | Dashboard and rewards | `GET /api/tier` | [Gamification](05-gamification-rewards-leaderboard.md) |
| Leaderboard | Dashboard card | `GET /api/leaderboard` | [Gamification](05-gamification-rewards-leaderboard.md) |
| Basic profile fields | `/profile` | `GET /api/me`, `PUT /api/profile` | [Profile and Goals](06-profile-goals-settings.md) |
| Weight entries | Dashboard summary | `POST /api/profile/weight` | [Profile and Goals](06-profile-goals-settings.md) |
| Monthly weight goal | Dashboard summary | `PUT /api/profile/weight-goal` | [Profile and Goals](06-profile-goals-settings.md) |
| Morning workout window | Scoring and reminders | `PUT /api/profile/morning-window` | [Profile and Goals](06-profile-goals-settings.md) |
| Kitchen-closed window | Reminder dispatcher | `PUT /api/profile/kitchen-window` | [Profile and Goals](06-profile-goals-settings.md) |
| HealthKit ingest contract | Error diagnostics mention HealthKit | `POST /api/healthkit/ingest` | [Integrations](07-integrations-notifications-recovery.md) |
| Health checks | Error diagnostics | `GET /health`, `GET /health/ready` | [Integrations](07-integrations-notifications-recovery.md) |
| Error and not-found states | `/error`, unmatched routes | problem responses | [Integrations](07-integrations-notifications-recovery.md) |

## Current Implementation Notes

- The Angular web app currently uses `https://localhost:5001` as its API base URL.
- The combined local startup scripts serve the frontend on `http://localhost:4321` by default.
- `npm start` in `frontend/` serves the frontend on Angular CLI's default port, usually `http://localhost:4200`.
- In development, the backend seeds a local account:
  - Email: `dev@forge.local`
  - Password: `DevPassword123!`
- Password reset email and notification delivery are MVP stand-ins that write log entries rather than sending real messages.
- HealthKit ingestion is currently a contract and logging stand-in. It accepts samples, but the real Apple Watch sync pipeline is deferred.

## Navigation

Authenticated users use the same four primary destinations throughout the app:

- Home: `/dashboard`
- Workouts: `/workouts`
- Rewards: `/rewards`
- Profile: `/profile`

On mobile-width screens the app shows these destinations in a bottom navigation bar. On desktop-width screens it switches to a left navigation rail.
