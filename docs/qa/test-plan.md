# ForgeFit Test Plan

This plan exercises the implemented app end-to-end against the L2
requirements. Each section traces back to one or more L2 IDs. A
scenario is **Pass** when the documented expected outcome is observed
and **Fail** otherwise — failures get logged to `./docs/bugs/pass-N.md`
during TP3.

Scenarios are grouped by feature area. Setup steps that recur are
defined once at the top.

## Setup

- A clean SQL Server database is reachable from the API. Run
  `./scripts/provision-azure.sh` (cloud) or follow `./docs/runbooks/local.md`
  (local).
- The web app is reachable on a known origin (`https://localhost:4200`
  in dev, the Static Web App URL in cloud).
- No prior account exists with the test email pattern
  `tp1-<scenario>-<timestamp>@forgefit.app`.
- Browser: latest stable Chrome unless otherwise specified.

## Auth + account

### S-001: Local user registration · L2-001
1. Visit `/sign-up`, fill name + email + password (`ForgeFit!2026`).
2. Submit.
3. **Expected:** redirected to `/dashboard`; greeting shows the email and `User`. The `Users` table contains a row with the expected email and a non-empty `PasswordHash`.

### S-002: Sign-in happy path · L2-002, L2-032
1. Register a user, then sign out.
2. Visit `/sign-in`, enter the email + password, click Sign in.
3. **Expected:** redirected to `/dashboard`; `Authorization: Bearer <jwt>` header observed on the next API call.

### S-003: Sign-out clears the session · L2-003
1. Signed-in user clicks `Sign out`.
2. **Expected:** routed to `/sign-in`; visiting `/dashboard` directly redirects back to `/sign-in?returnUrl=/dashboard`.

### S-004: Password reset email no-op · L2-004
1. Visit `/password-reset`, enter an email (registered or not), submit.
2. **Expected:** the same confirmation message appears either way (no enumeration). Server log line `password-reset.email.deferred email=… token=…` is emitted.

### S-005: Password reset confirm leg · L2-004
1. Capture a token from the deferred-email log line; visit `/password-reset/confirm?token=<token>`.
2. Submit a new strong password.
3. **Expected:** `204 NoContent`; navigates to `/sign-in`. Old password no longer works; new password does.

### S-006: Profile read · L2-005
1. Signed-in user opens `/profile`.
2. **Expected:** form pre-populates with the user's first/last name, email, units, time zone, daily targets — sourced from `GET /api/me`.

### S-007: Profile update persists · L2-005, L2-014, L2-016, L2-017, L2-026, L2-027
1. On `/profile`, change first name, time zone, daily calorie target, monthly weight goal, morning + kitchen windows, leaderboard opt-in.
2. Save.
3. **Expected:** all fields persist after a hard reload; `GET /api/me` reflects each change.

### S-008: Account deletion · L2-006, L2-050
1. On `/profile`, click Delete account, then Yes, delete my account.
2. **Expected:** routed to `/sign-in`. `User` row has `IsDeleted = true` and `DeletedAt` populated. Re-sign-in with the same credentials returns 401.

## Sessions

### S-009: Manual session log · L2-007
1. From `/workouts/new` create a Treadmill session: 22 min, 2.1 mi, 128 bpm, 218 kcal.
2. **Expected:** redirected to `/dashboard`; daily ring includes the 218 kcal contribution; `/workouts` shows the new row.

### S-010: Validation boundaries · L2-007, L2-039, L2-040
1. Try durations `0`, `481`, calories `5001`, HR `29`, HR `241`.
2. **Expected:** each rejected with a 400 ProblemDetails carrying the ValidationProblemDetails errors object (or the form blocks submission client-side).

### S-011: List + filter sessions · L2-008
1. Seed 10 sessions across the four equipment types.
2. On `/workouts`, click Treadmill chip.
3. **Expected:** only treadmill rows visible. Click `This week` chip → only sessions in the last 7 days. Click `All` → all rows.

### S-012: Edit session refunds + re-scores · L2-009, L2-018, L2-020
1. Create a 22-min treadmill session (+44 Base).
2. Open `/workouts/:id`, change duration to 30, save.
3. **Expected:** ledger contains the original `+44 Base`, a `-44 Refund`, and a fresh `+60 Base`. Net for the session is +60.

### S-013: Duplicate session · L2-009
1. Open an old session, click Duplicate.
2. **Expected:** new row in `/workouts` stamped at "now"; ledger has fresh +Base for the duplicate; original untouched.

### S-014: Delete session refund · L2-009
1. Create a +44 session, delete it from `/workouts/:id`.
2. **Expected:** routed to `/dashboard`. `Tier card` balance returns to the pre-session value. Ledger keeps the `+44 Base` (tombstoned to the deleted session id) plus a `-44 Refund`.

### S-015: Equipment list · L2-010
1. `GET /api/equipment` (or open `/workouts/new`).
2. **Expected:** four rows in stable order — Treadmill, IndoorBike, BenchPress, Elliptical.

## Dashboard + summary

### S-016: Daily ring + minutes tile · L2-011, L2-012
1. Sign in; navigate to `/dashboard`.
2. **Expected:** ring displays today's calories vs 1500 kcal default. Minutes tile shows today's minutes vs 60 min default.

### S-017: Dashboard composes all five cards · L2-013
1. Open `/dashboard`.
2. **Expected:** Daily ring, Streak, Weight progress, Tier, Leaderboard cards all visible.

### S-018: Dashboard latency budget · L2-042
1. With at least one session logged, time `GET /api/dashboard` from a warmed-up server.
2. **Expected:** under 200 ms locally (rough budget; gracious ceiling of 1500 ms in CI to absorb cold-start).

## Weight goal + entries

### S-019: Set monthly weight goal · L2-014
1. From `/profile` set monthly goal to 15.
2. **Expected:** persists; weight progress card on dashboard shows `−15 lb / month`. Out-of-range (0 or 31) returns 400.

### S-020: Record current weight · L2-015
1. POST `/api/profile/weight` with `weightLb: 195.4`, then `194.8`.
2. **Expected:** both rows persist in `WeightEntries` ordered by `RecordedAt`. Posting `0` returns 400.

## Behavioral windows + nudges

### S-021: Update morning window + reminder · L2-016, L2-025
1. From `/profile` set window 05:00–08:00 with reminder enabled.
2. **Expected:** persists. Notification dispatcher emits `notification.morning userId=<id>` log line within 2 minutes of 05:00 user-local.

### S-022: Update kitchen window + nudge · L2-017, L2-026
1. Set window 21:00–05:30 with nudge enabled.
2. **Expected:** persists; equal endpoints (e.g. 20:00–20:00) returns 400. Span-midnight is allowed. Dispatcher emits `notification.kitchen` near 21:00.

## Gamification

### S-023: Base points scoring · L2-018
1. Create a 22-min Treadmill session at NY 14:00 (no morning bonus, day-1 streak).
2. **Expected:** ledger has exactly one `+44 Base` row tied to the session id.

### S-024: Morning bonus · L2-019
1. Create a session at 05:12 NY local (default window 05:00–07:30).
2. **Expected:** ledger has `+44 Base` and `+25 MorningBonus`. Same session shifted to 09:00 produces only Base.

### S-025: Streak multiplier · L2-020
1. Log 7 consecutive daily sessions (FakeClock advances per day).
2. **Expected:** day 7 gets a `+3 StreakMultiplier ×1.07`. Skipping a day resets the streak to 1.

### S-026: Rewards catalog · L2-021
1. Open `/rewards`.
2. **Expected:** seeded rewards visible (Smoothie, Rest Day Pass, etc.). Inactive rewards are hidden.

### S-027: Reward redemption · L2-021
1. Seed +1000 ledger; redeem the 200-pt Smoothie.
2. **Expected:** balance flips to 800. `RewardRedemption` row created. `-200 Redemption` ledger row tied to the redemption id.

### S-028: Insufficient balance · L2-021
1. With balance 50, attempt to redeem the 12000-pt Running Shoes via API.
2. **Expected:** 400 with `title: "INSUFFICIENT_POINTS"`. No state change.

### S-029: Tier promotion · L2-022
1. Seed +4999 lifetime points → check `/api/tier`.
2. **Expected:** `Silver`. Add a 1-min mid-day session → 5001 lifetime → tier promotes to `Forged Iron`.

## Apple Watch / HealthKit (deferred integration)

### S-030: HealthKit ingest stub · L2-023
1. POST `/api/healthkit/ingest` with `{ sampleType, value, unit, recordedAt }`.
2. **Expected:** 202 Accepted. Server log line `healthkit.ingest.deferred userId=…`.

### S-031: Sync error surface · L2-024, L2-029
1. Visit `/error?traceId=abc123`.
2. **Expected:** SyncErrorPanel renders with the trace id, a Backend status badge (Healthy/Unhealthy), and a HealthKit ingest "Deferred" badge. `Go to dashboard` CTA navigates correctly.

## Leaderboard

### S-032: Leaderboard opt-in flag · L2-027
1. New user sees themselves only on `/api/leaderboard` (always include self). Toggle opt-in on; another opted-in user now sees them. Toggle off; they disappear from peers.
2. **Expected:** behavior matches.

## Empty + error states

### S-033: Workouts empty state · L2-028
1. Brand-new account opens `/workouts`.
2. **Expected:** `forge-empty-state` visible with "No sessions yet" + "Log your first session" CTA.

### S-034: 404 unknown route · L2-028
1. Visit `/does-not-exist`.
2. **Expected:** NotFoundPage renders with "Page not found" + "Go to dashboard" CTA.

### S-035: 5xx error surface · L2-029
1. Drop the SQL DB (or simulate via `/health/ready` returning 503).
2. **Expected:** Either `/error?traceId=…` is reachable, or the dashboard surfaces a friendly error toast/banner. Trace id is copyable.

## Responsive layout

### S-036: Breakpoints · L2-030
1. Visit `/dashboard` at 360px, 768px, 1440px viewports.
2. **Expected:** at 360 the bottom nav strip shows; at ≥992 the side nav rail shows. No horizontal scroll. All `data-testid` hooks remain reachable.

## Auth security

### S-037: bcrypt password hash · L2-031
1. Inspect `Users.PasswordHash` after registration.
2. **Expected:** starts with `$2a$` / `$2b$` (bcrypt). Plain password is never visible in the column.

### S-038: JWT issuance + validation · L2-032
1. Sign in; capture the `accessToken`. Decode with jwt.io.
2. **Expected:** `iss` = configured issuer, `aud` = configured audience, `sub` = user id, `role` claim present, `exp` ~ 1 hour.

### S-039: Refresh-token rotation · L2-033
1. Sign in; immediately call `POST /api/auth/refresh` with the refresh token.
2. **Expected:** 200 with a new pair; the old refresh token now fails (replay-detection: family revoked).

### S-040: Sign-in throttling · L2-034
1. Submit 6 wrong passwords in 60 seconds for the same email.
2. **Expected:** the 6th attempt returns 429 with `Retry-After` header.

### S-041: Audit log entries · L2-035
1. Sign in, sign out, fail a sign-in.
2. **Expected:** rows in `AuditLog` for `sign-in.success`, `sign-out`, `sign-in.failed` with the user id (where known) and IP/UA.

### S-042: External IdPs absent · L2-036
1. Inspect sign-in / sign-up screens.
2. **Expected:** no Google/Apple buttons. Pure local username/password.

### S-043: Roles enforce policy · L2-037, L2-038
1. Confirm `[Authorize]` is on every controller except `/health`, `/health/ready`, `/api/equipment`, `/api/auth/*`.
2. **Expected:** anonymous calls to authed endpoints get 401.

### S-044: Validation flows · L2-039, L2-040
1. Submit a malformed `RegisterRequest` (empty email).
2. **Expected:** 400 with `application/problem+json` `ValidationProblemDetails` listing `Email` errors.

### S-045: API response-time targets · L2-041
1. Time core endpoints (`/api/auth/sign-in`, `/api/sessions` POST/GET, `/api/dashboard`) on a warmed server.
2. **Expected:** p50 under the documented target (~150 ms locally for read paths, ~250 ms for write paths). CI slack: 1500 ms.

## Observability

### S-046: Structured logging · L2-043, L2-051
1. Trigger a sign-in with a distinctive password.
2. **Expected:** stdout JSON contains `request.handled` lines with `Method`, `Path`, `Status`, `TraceId`, `UserId`. Grep for the password text → zero matches across all log output.

### S-047: Health endpoints · L2-044
1. `GET /health` → 200 always; `GET /health/ready` → 200 when DB reachable, 503 when DB unreachable.
2. **Expected:** both behaviors observed.

## Accessibility

### S-048: WCAG 2.1 AA · L2-045
1. Run the `accessibility.spec.ts` axe scan over each route.
2. **Expected:** zero violations.

### S-049: Touch targets · L2-046
1. Inspect every interactive control (button, chip, icon button) at 360px.
2. **Expected:** ≥48×48 dp.

### S-050: Keyboard navigation · L2-047
1. Tab through `/dashboard`, `/workouts`, `/workout-detail`, `/profile`.
2. **Expected:** focus order is logical; visible focus ring on every focusable; Enter activates buttons; Esc closes any popover.

### S-051: Color contrast · L2-048
1. Run the contrast audit pass of axe (covered by S-048) and spot-check brand-color combinations.
2. **Expected:** ≥4.5:1 for body text, ≥3:1 for large text and UI elements.

## Data protection

### S-052: PII in transit + at rest · L2-049
1. Inspect production traffic with Wireshark / curl `--verbose`.
2. **Expected:** all API traffic over TLS 1.2+. Database connections use `Encrypt=True`.

### S-053: Account deletion full purge · L2-050
1. Delete account from `/profile`.
2. **Expected:** `User.IsDeleted = true`, `DeletedAt` populated. Sessions, ledger, redemptions remain (linked to user id) for the audit-log retention window — see security policy.

### S-054: No plaintext secrets in logs · L2-051
1. Same as S-046.
2. **Expected:** zero matches for the test password, zero matches for any access/refresh token text in stdout.

### S-055: XSS / output encoding · L2-052
1. Register with a name like `<script>alert(1)</script>`. Render it on the dashboard greeting.
2. **Expected:** Angular escapes the script tag — text appears as literal characters; no alert fires.

## CI + dependencies

### S-056: CI dep vuln scan · L2-053
1. Inspect `.github/workflows/ci.yml`.
2. **Expected:** `dotnet list package --vulnerable --include-transitive` step fails on high/critical; `npm audit --audit-level=high` runs on production deps.

## Architecture

### S-057: Clean Architecture + CQS + one-type-per-file · L2-054
1. Spot-check `Forge.Application` for one-public-type-per-file. Spot-check that no controller talks to EF directly. Spot-check `Forge.Infrastructure` for the only references to EF / SqlClient / bcrypt.
2. **Expected:** all three rules hold.

## Regression scenarios

### R-001: Refresh-token replay
After S-039, attempt to refresh again with the *original* token. **Expected:** 401 + audit entry `refresh.replay-detected`.

### R-002: Concurrent same-day session create + delete
Create a session, immediately delete it, immediately create another at the same timestamp. **Expected:** ledger nets to the second session's score; no orphan rows.

### R-003: Session edit while offline
Edit a session, kill the network mid-PUT, recover. **Expected:** retry with the same payload succeeds; no double Refund row.

### R-004: Browser back navigation after delete
Delete a session, click browser back. **Expected:** the detail page either renders 404 or routes to `/workouts`; never a blank page.

### R-005: Time-zone change mid-streak
User streak hits 5 days while in `America/New_York`; they switch to `Asia/Tokyo`. **Expected:** the next session in Tokyo still increments the streak — IClock.TodayInTimeZone respects the new zone.

## Edge cases

### E-001: 480-minute session duration boundary
Submit `durationMinutes: 480`. **Expected:** accepted; +960 Base ledger row.

### E-002: 1-minute session boundary
Submit `durationMinutes: 1`. **Expected:** accepted; +2 Base ledger row.

### E-003: Notes at 2000 chars exactly
Submit `notes` of exactly 2000 chars. **Expected:** accepted; 2001 chars rejected.

### E-004: Reward at exact balance
Balance == reward cost. **Expected:** redemption succeeds, balance becomes 0.

### E-005: Tier exactly at threshold
Lifetime points == 5000. **Expected:** tier reads `Forged Iron` (inclusive lower bound).

### E-006: Empty leaderboard for sole user
First user signs up, opts in, looks at `/api/leaderboard`. **Expected:** rank 1 with their own record.

## Coverage matrix

Every L2 ID maps to at least one S- or R- scenario above:

| L2 ID  | Scenarios |
|--------|-----------|
| L2-001 | S-001 |
| L2-002 | S-002 |
| L2-003 | S-003 |
| L2-004 | S-004, S-005 |
| L2-005 | S-006, S-007 |
| L2-006 | S-008 |
| L2-007 | S-009, S-010 |
| L2-008 | S-011 |
| L2-009 | S-012, S-013, S-014 |
| L2-010 | S-015 |
| L2-011 | S-016 |
| L2-012 | S-016 |
| L2-013 | S-017 |
| L2-014 | S-007, S-019 |
| L2-015 | S-020 |
| L2-016 | S-007, S-021 |
| L2-017 | S-007, S-022 |
| L2-018 | S-012, S-023 |
| L2-019 | S-024 |
| L2-020 | S-012, S-025 |
| L2-021 | S-026, S-027, S-028 |
| L2-022 | S-029 |
| L2-023 | S-030 |
| L2-024 | S-031 |
| L2-025 | S-021 |
| L2-026 | S-007, S-022 |
| L2-027 | S-007, S-032 |
| L2-028 | S-033, S-034 |
| L2-029 | S-031, S-035 |
| L2-030 | S-036 |
| L2-031 | S-037 |
| L2-032 | S-002, S-038 |
| L2-033 | S-039, R-001 |
| L2-034 | S-040 |
| L2-035 | S-041 |
| L2-036 | S-042 |
| L2-037 | S-043 |
| L2-038 | S-043 |
| L2-039 | S-010, S-044 |
| L2-040 | S-044 |
| L2-041 | S-045 |
| L2-042 | S-018 |
| L2-043 | S-046 |
| L2-044 | S-047 |
| L2-045 | S-048 |
| L2-046 | S-049 |
| L2-047 | S-050 |
| L2-048 | S-051 |
| L2-049 | S-052 |
| L2-050 | S-008, S-053 |
| L2-051 | S-046, S-054 |
| L2-052 | S-055 |
| L2-053 | S-056 |
| L2-054 | S-057 |
