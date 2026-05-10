# Backend implementation tasks

Vertical-slice task list for `BI1`. Each task implements a single end-to-end slice on top of the MB1 MVP and against `./docs/plans/backend.md`. Per-task contents:

- **Requirements** — L2 IDs the slice satisfies.
- **Slice** — exactly the artifacts that must change or be added.
- **Acceptance test** — the Playwright POM (or backend integration) test that gates the slice. Test header carries `// Traces to: <L2-IDs>`.
- **Guidance** — Implementation Guidance bullets that apply.

Slices are sequenced by dependency. Where two slices have no dependency, they may be picked in either order in BI1.

Conventions:
- Every command has a colocated `<CommandName>Validator.cs`. Queries do not have validators.
- Handlers depend on `IAppDbContext` + `ICurrentUser` + feature-specific abstractions only. No repository, no unit-of-work.
- `ExceptionHandlingMiddleware` already maps `ValidationException` → 400 `application/problem+json`; new exceptions get a new catch block in that middleware.
- Each slice creates / updates files under `backend/src/Forge.{Domain,Application,Infrastructure,Api}/...` only — never touches another slice's code.

## Phase BI1.1 — Migration foundation, sessions CRUD, auth completeness

### BT-001 — Initial EF migration replacing `EnsureCreated` ✅ done

- **Requirements:** L2-001..L2-002, L2-031..L2-038, L2-049 (migration baseline).
- **Slice:**
  - Add `Microsoft.EntityFrameworkCore.Tools` to `Forge.Infrastructure.csproj`.
  - Generate migration `Initial_AuthAndSessions` capturing the MB1 schema (`Users`, `WorkoutSessions`) plus the empty tables for `RefreshTokens`, `SignInAttempts`, `AuditLog`, `PasswordResetTokens`. Add the matching domain types (one type per file).
  - Replace `db.Database.EnsureCreated()` in `Program.cs` with `await db.Database.MigrateAsync()`.
  - Add `dotnet ef database update` instructions to `docs/runbooks/backend.md`.
- **Acceptance test:** `tests/Forge.Acceptance/Database/MigrationsAcceptanceTest.cs` — boots the API against a fresh LocalDB instance and asserts every required table exists by querying `INFORMATION_SCHEMA.TABLES`.
- **Guidance:** Backend (EF Core, Clean Architecture), General (one type per file).

### BT-002 — Refresh token issuance + rotation ✅ done

- **Requirements:** L2-002 (refresh path), L2-033.
- **Slice:**
  - `Forge.Domain/RefreshToken.cs` (already added in BT-001).
  - `Forge.Application/Auth/RefreshTokenCommand.cs`, `RefreshTokenCommandValidator.cs`, `RefreshTokenCommandHandler.cs`.
  - `Forge.Application/Abstractions/IRefreshTokenStore.cs` (`Issue`, `Consume`, `RevokeFamily` methods).
  - `Forge.Infrastructure/RefreshTokenStore.cs` — SHA-256-hashes incoming tokens, scans by hash, rotates atomically via `SaveChangesAsync`.
  - `RegisterCommandHandler` and `SignInCommandHandler` extended to issue a refresh token on success and return it in `AuthResult` (add `RefreshToken` property).
  - `AuthController` adds `POST /api/auth/refresh`.
- **Acceptance test:** `RefreshTokenAcceptanceTest` — registers a user, exchanges the refresh token once (succeeds), replays the consumed refresh token (`401` and entire family revoked → next valid attempt also `401`).
- **Guidance:** Authentication, Backend (CQS, IAppDbContext).

### BT-003 — Sign-out ✅ done

- **Requirements:** L2-003.
- **Slice:**
  - `Forge.Application/Auth/SignOutCommand.cs` + validator + handler. Handler revokes the supplied refresh token + family via `IRefreshTokenStore.RevokeFamily`.
  - `AuthController` adds `POST /api/auth/sign-out` (`[Authorize]`).
- **Acceptance test:** sign in, sign out, attempt to refresh → `401`.
- **Guidance:** Authentication.

### BT-004 — Password reset request (no-op email) ✅ done

- **Requirements:** L2-004 (request leg).
- **Slice:**
  - `Forge.Application/Auth/RequestPasswordResetCommand.cs` + validator + handler.
  - `Forge.Application/Abstractions/IPasswordResetEmailSender.cs`.
  - `Forge.Infrastructure/Deferred/LoggingPasswordResetEmailSender.cs` — logs the intended action under category `Forge.Auth.PasswordReset`.
  - `Forge.Domain/PasswordResetToken.cs`.
  - `AuthController` adds `POST /api/auth/password-reset/request`. Always responds `202` regardless of whether the email exists (no enumeration).
  - Documented in `backend.md` under "deferred integrations".
- **Acceptance test:** request reset for unknown email → `202`; request for known email → `202` and a row inserted into `PasswordResetTokens`.
- **Guidance:** Authentication, General (deferred-integration shape).

### BT-005 — Password reset confirm ✅ done

- **Requirements:** L2-004 (confirm leg), L2-031.
- **Slice:**
  - `Forge.Application/Auth/ConfirmPasswordResetCommand.cs` + validator (token + new password meeting policy) + handler. Handler validates token + ttl + not-consumed, hashes new password, marks token consumed, revokes all refresh tokens for the user, audits `password-reset.success`.
  - `AuthController` adds `POST /api/auth/password-reset/confirm`.
- **Acceptance test:** valid token → password updated, sign-in with new password works, all prior refresh tokens are revoked. Reused / expired tokens → `400`.
- **Guidance:** Authentication.

### BT-006 — Account deletion ✅ done

- **Requirements:** L2-006, L2-050.
- **Slice:**
  - `Forge.Application/Auth/DeleteAccountCommand.cs` + validator (no payload — uses `ICurrentUser`) + handler. Handler soft-deletes user, anonymizes `FirstName`/`LastName`/`Email` to a sentinel `deleted+<userId>@forgefit.local`, revokes all refresh tokens, audits `account.delete`.
  - `MeController` adds `DELETE /api/me`.
- **Acceptance test:** delete account, attempt sign-in → `401`. Inspect row → email is the sentinel pattern.
- **Guidance:** Authentication, Data protection.

### BT-006a — `IClock` abstraction ✅ done

- **Requirements:** Cross-cutting test seam for time-dependent slices (L2-016, L2-017, L2-019, L2-020, L2-024, L2-025, L2-026, L2-029, L2-034).
- **Slice:**
  - `Forge.Application/Abstractions/IClock.cs` exposing `DateTimeOffset UtcNow { get; }` and `DateOnly TodayInTimeZone(string ianaTimeZoneId)`.
  - `Forge.Infrastructure/SystemClock.cs` — singleton, returns `DateTimeOffset.UtcNow` and computes today using `TimeZoneInfo.ConvertTimeBySystemTimeZoneId`.
  - DI registration as `Singleton` in `Forge.Infrastructure/DependencyInjection.cs`.
  - Every later slice that names a time boundary (BT-007 throttle window, BT-018/BT-019 window validation, BT-022/BT-023/BT-024 scoring, BT-029 dashboard "today", BT-032 notification dispatcher) reads time via `IClock` rather than `DateTimeOffset.UtcNow`.
- **Acceptance test:** `ClockSeamAcceptanceTest` — register a `FakeClock` in the DI container, call any time-dependent handler, assert the persisted timestamp matches the fake's `UtcNow` rather than wall time.
- **Guidance:** Backend (no clock-coupling); General (testability).

### BT-007 — Sign-in throttling and lockout ✅ done

- **Requirements:** L2-034.
- **Slice:**
  - `Forge.Domain/SignInAttempt.cs` (added in BT-001 schema).
  - `Forge.Application/Abstractions/ISignInThrottle.cs` (`Task<ThrottleDecision> Check(string email, string ip, CancellationToken)`).
  - `Forge.Infrastructure/SignInThrottle.cs` — counts `SignInAttempts` for the email in a rolling 15-minute window measured via `IClock.UtcNow`; if `≥ 5` failures, returns `Locked(retryAfter)`.
  - `SignInCommandHandler` calls `ISignInThrottle.Check` first; on lock, throws `SignInLockedException` mapped to `429` in `ExceptionHandlingMiddleware`.
  - Handler also writes `SignInAttempt` rows on success and failure paths.
- **Acceptance test:** 5 failed sign-ins in a row → 6th returns `429` even with the correct password. Advance the `FakeClock` (BT-006a) by 16 minutes, correct password works again.
- **Guidance:** Authentication, Backend.

### BT-008 — Security audit log ✅ done

- **Requirements:** L2-035, L2-043 (related logging concern).
- **Slice:**
  - `Forge.Domain/AuditLog.cs`.
  - `Forge.Application/Abstractions/IAuditLogger.cs` (`Task Write(string @event, Guid? userId, string? ip, string? userAgent, object? payload, CancellationToken)`).
  - `Forge.Infrastructure/AuditLogger.cs` — appends to `AuditLog`. Implementation strips known-secret keys from the `payload` JSON before persisting.
  - Wire into `RegisterCommandHandler`, `SignInCommandHandler`, `SignOutCommandHandler`, `RefreshTokenCommandHandler`, `ConfirmPasswordResetCommandHandler`, `DeleteAccountCommandHandler`.
- **Acceptance test:** failed sign-in writes one `sign-in.failure` row; successful sign-in writes one `sign-in.success` row; password value never appears in any audit row's payload.
- **Guidance:** Authentication, Observability.

### BT-009 — List sessions with filters ✅ done

- **Requirements:** L2-008.
- **Slice:**
  - `Forge.Application/Sessions/ListSessionsQuery.cs` (filters: equipment?, range: today | week | month | all, search?, page, pageSize) + handler. Uses `.AsNoTracking()` and projects to `SessionDto`.
  - `SessionsController` adds `GET /api/sessions` accepting filter query parameters.
  - Composite index `WorkoutSessions(UserId, StartedAt)` already added in BT-001 migration.
- **Acceptance test:** seed 10 sessions across the four equipment types, query `?equipment=Treadmill` → only treadmill rows; query `?range=week` → only sessions in last 7 days; query `?search=zone` → only rows with `Notes` containing "zone".
- **Guidance:** Backend (CQS, IAppDbContext, performance).

### BT-010 — Update session

- **Requirements:** L2-009 (update leg).
- **Slice:**
  - `Forge.Application/Sessions/UpdateSessionCommand.cs` + validator + handler. Handler updates fields; if equipment / duration / start-time changed, deletes prior `PointsLedger` rows for the session and re-scores via `IPointsScorer` (introduced in BT-022).
  - `SessionsController` adds `PUT /api/sessions/{id}`.
- **Acceptance test:** update a session's duration; assert ledger rows reflect new base points. Update notes only; assert ledger rows unchanged.
- **Guidance:** Backend (CQS).

### BT-011 — Duplicate session

- **Requirements:** L2-009 (duplicate leg).
- **Slice:**
  - `Forge.Application/Sessions/DuplicateSessionCommand.cs` + validator + handler. Inserts a new `WorkoutSession` with `StartedAt = today (user TZ)` and copies the rest. Scores fresh ledger rows.
  - `SessionsController` adds `POST /api/sessions/{id}/duplicate`.
- **Acceptance test:** duplicate a session; new row appears with today's date and unchanged equipment/duration; ledger has fresh +base rows for the duplicate.
- **Guidance:** Backend.

### BT-012 — Delete session with point refund

- **Requirements:** L2-009 (delete leg).
- **Slice:**
  - `Forge.Application/Sessions/DeleteSessionCommand.cs` + validator + handler. Handler appends compensating `PointsLedger` rows (`Reason = Refund`) summing to the negative of awarded points, then deletes the session.
  - `SessionsController` adds `DELETE /api/sessions/{id}`.
- **Acceptance test:** create a 22-min treadmill session (+44 base points). Delete it. Assert balance is back to pre-session value.
- **Guidance:** Backend.

### BT-013 — List equipment ✅ done

- **Requirements:** L2-010.
- **Slice:**
  - `Forge.Application/Equipment/ListEquipmentQuery.cs` + handler returns the four enum values + display names.
  - `EquipmentController` adds `GET /api/equipment`.
- **Acceptance test:** four entries returned in stable order.
- **Guidance:** Backend.

## Phase BI1.2 — Profile, weight, behavioral windows

### BT-014 — Get current user ✅ done

- **Requirements:** L2-005 (read leg).
- **Slice:**
  - `Forge.Application/Auth/GetCurrentUserQuery.cs` + handler returning `CurrentUserDto` (id, email, role, all profile fields).
  - `MeController` adds `GET /api/me`.
- **Acceptance test:** sign in, `GET /api/me` returns the user's email, role `User`, and default profile values.
- **Guidance:** Backend.

### BT-015 — Profile migration + update ✅ done

- **Requirements:** L2-005, L2-014, L2-016, L2-017, L2-026, L2-027.
- **Slice:**
  - Migration `AddProfileAndWeight` adds profile columns to `Users` (`Units`, `TimeZoneId`, `DailyActiveCaloriesTarget` default 1500, `DailyWorkoutMinutesTarget` default 60, `MonthlyWeightGoalLb` default 20, `MorningWindowStart` default `05:00`, `MorningWindowEnd` default `07:30`, `KitchenClosedStart` default `20:00`, `KitchenClosedEnd` default `06:00`, `KitchenNudgeEnabled` default true, `MorningReminderEnabled` default true, `LeaderboardOptIn` default false) and creates `WeightEntries` table.
  - `Forge.Application/Profile/UpdateProfileCommand.cs` + validator (lengths, IANA time-zone id check, target ranges) + handler.
  - `ProfileController` adds `PUT /api/profile`.
- **Acceptance test:** update profile fields; `GET /api/me` reflects them; changing email to an existing one returns `409`.
- **Guidance:** Backend, Validation.

### BT-016 — Record current weight ✅ done

- **Requirements:** L2-015.
- **Slice:**
  - `Forge.Domain/WeightEntry.cs` (added in BT-015 migration).
  - `Forge.Application/Profile/RecordCurrentWeightCommand.cs` + validator (>0, ≤1500 lb) + handler.
  - `ProfileController` adds `POST /api/profile/weight`.
- **Acceptance test:** post 195.4 → row appended; post a second value 194.8 → both rows persist.
- **Guidance:** Backend.

### BT-017 — Set monthly weight goal ✅ done

- **Requirements:** L2-014.
- **Slice:**
  - `Forge.Application/Profile/SetMonthlyWeightGoalCommand.cs` + validator (1..30 lb / month) + handler.
  - `ProfileController` adds `PUT /api/profile/weight-goal`.
- **Acceptance test:** valid goal stores; out-of-range goal returns `400`.
- **Guidance:** Validation.

### BT-018 — Update morning window ✅ done

- **Requirements:** L2-016.
- **Slice:**
  - `Forge.Application/Profile/UpdateMorningWindowCommand.cs` + validator (start < end, both within `[00:00, 23:59]`) + handler.
  - `ProfileController` adds `PUT /api/profile/morning-window`.
- **Acceptance test:** update window to 05:00–08:00; `GET /api/me` reflects.
- **Guidance:** Validation.

### BT-019 — Update kitchen window + nudge toggle ✅ done

- **Requirements:** L2-017, L2-026.
- **Slice:**
  - `Forge.Application/Profile/UpdateKitchenWindowCommand.cs` + validator + handler.
  - `ProfileController` adds `PUT /api/profile/kitchen-window`.
- **Acceptance test:** update window + toggle nudge off; `GET /api/me` reflects.
- **Guidance:** Validation.

### BT-020 — Set leaderboard opt-in ✅ done

- **Requirements:** L2-027.
- **Slice:**
  - `Forge.Application/Profile/SetLeaderboardOptInCommand.cs` + validator + handler.
  - `ProfileController` adds `PUT /api/profile/leaderboard-opt-in`.
- **Acceptance test:** toggle on, list leaderboard (BT-030) — row appears; toggle off — row disappears.
- **Guidance:** Validation.

## Phase BI1.3 — Gamification

### BT-021 — *(merged into BT-022)*

Originally enumerated as a standalone migration-only task. BT2 Pass 1 found it had no end-to-end value on its own; the migration ships alongside the first scorer slice instead. This ID is reserved for cross-references and intentionally left empty.

### BT-022 — Migration `AddPointsAndRewards` + base points scoring + `IPointsScorer` ✅ done

- **Requirements:** L2-018, L2-021 (read leg foundation), L2-022 (read leg foundation).
- **Slice:**
  - Migration `AddPointsAndRewards` adds `PointsLedger`, `RewardCatalogItem`, `RewardRedemption`. Seeds the rewards catalog (5–10 rows: smoothie, rest-day pass, new socks, etc.).
  - Domain types: `PointsLedger`, `PointsLedgerReason` enum, `RewardCatalogItem`, `RewardRedemption`.
  - `IAppDbContext` gains the three `DbSet`s.
  - `Forge.Application/Gamification/ScoringConstants.cs` — `BasePointsPerMinute = 2`, `MorningBonusPoints = 25`, streak formula constants, tier thresholds.
  - `Forge.Application/Abstractions/IPointsScorer.cs` (`Task Score(WorkoutSession session, CancellationToken)`).
  - `Forge.Infrastructure/PointsScorer.cs` — appends a `Base` ledger row of `BasePointsPerMinute × DurationMinutes` for the session. (Morning bonus + streak multiplier extend this in BT-023 / BT-024.)
  - `CreateSessionCommandHandler` calls `_scorer.Score(session, ct)` after `SaveChangesAsync` returns.
- **Acceptance test:** create a 22-min treadmill session against a fresh DB; assert `RewardCatalogItem` has the seeded rows AND the `PointsLedger` has one row `+44 (Base — 22 min logged)` for the user. End-to-end coverage of the migration plus the first behavioral scorer.
- **Guidance:** Backend (EF migrations, CQS, one type per file).

### BT-023 — Morning bonus ✅ done

- **Requirements:** L2-019.
- **Slice:**
  - Extend `PointsScorer.Score` to check whether `session.StartedAt` (in the user's local time zone) falls within `[MorningWindowStart, MorningWindowEnd]`; if so, append `+25 (Morning bonus)` ledger row.
- **Acceptance test:** session at 05:12 with default window → `+44` and `+25` rows appear; session at 09:00 → only `+44` row.
- **Guidance:** Backend.

### BT-024 — Streak multiplier ✅ done

- **Requirements:** L2-020.
- **Slice:**
  - Extend `PointsScorer.Score` to compute `consecutiveDays` by querying distinct calendar dates of prior `WorkoutSessions` for the user, evaluated in the user's `User.TimeZoneId` via `IClock.TodayInTimeZone(...)` (BT-006a). Apply the formula `multiplier = min(1.50, 1.00 + 0.01 × days)`. If multiplier > 1.00, append a `StreakMultiplier` ledger row of `floor(basePoints × (multiplier - 1.00))`.
- **Acceptance test:** seed 7 consecutive days of 22-min sessions in the user's local time zone (`FakeClock` advances per day); the 7th session writes `+44`, `+25`-or-not, `+6 (Streak multiplier ×1.07)`. Skip a day; the next session resets to multiplier 1.00.
- **Guidance:** Backend.

### BT-025 — Refunds on update / delete

- **Requirements:** L2-009, L2-018..L2-020 (refund leg).
- **Slice:**
  - Already covered functionally by BT-010 and BT-012. This task validates that `IPointsScorer` exposes a `Refund(sessionId)` method that appends compensating rows summing to negative the awarded amount, and that the `Update`/`Delete` handlers call it before the new score / before the delete respectively.
- **Acceptance test:** create a session, modify duration from 22 → 30; ledger has `-44 (Refund)` and `+60 (Base)`; net balance reflects the new total.
- **Guidance:** Backend.

### BT-026 — List rewards catalog

- **Requirements:** L2-021 (read leg).
- **Slice:**
  - `Forge.Application/Rewards/ListRewardsQuery.cs` + handler returning active catalog items.
  - `RewardsController` adds `GET /api/rewards`.
- **Acceptance test:** seeded rewards listed; inactive items hidden.
- **Guidance:** Backend.

### BT-027 — Redeem reward

- **Requirements:** L2-021.
- **Slice:**
  - `Forge.Application/Rewards/RedeemRewardCommand.cs` + validator + handler. Handler recomputes balance from `PointsLedger`. If insufficient, throws `InsufficientPointsException` (mapped to 400 with title `INSUFFICIENT_POINTS`). Else inserts `RewardRedemption` and a `-cost` ledger row.
  - `RewardsController` adds `POST /api/rewards/{id}/redeem`.
- **Acceptance test:** redeem with sufficient balance → 200, balance decreases by cost, redemption row created. Redeem with insufficient balance → 400 `INSUFFICIENT_POINTS`, no balance change.
- **Guidance:** Backend, Validation (custom error code).

### BT-028 — Get current tier

- **Requirements:** L2-022.
- **Slice:**
  - `Forge.Application/Rewards/GetCurrentTierQuery.cs` + handler computing tier from cumulative ledger using `ScoringConstants.TierThresholds`.
  - `RewardsController` adds `GET /api/tier`.
- **Acceptance test:** seed ledger to 4,999 lifetime points → tier `Silver`. Add a session that pushes to 5,001 → tier `Forged Iron`.
- **Guidance:** Backend.

### BT-029 — Dashboard summary

- **Requirements:** L2-011, L2-012, L2-013.
- **Slice:**
  - `Forge.Application/Dashboard/GetDashboardSummaryQuery.cs` + handler. Single read aggregating: today's active calories, today's minutes, current streak (distinct calendar days), current points balance, current tier, next reward within reach (cheapest catalog item ≥ balance), month-to-date weight delta vs goal.
  - `DashboardController` adds `GET /api/dashboard`.
- **Acceptance test:** seed user with 980 active kcal today, target 1500 → `caloriesToday = 980`, `targetCalories = 1500`. Dashboard responds in ≤ 200 ms locally (rough perf check).
- **Guidance:** Backend (performance — single query, projection only).

## Phase BI1.4 — Leaderboard, deferred integrations, hardening

### BT-030 — List leaderboard

- **Requirements:** L2-027.
- **Slice:**
  - `Forge.Application/Profile/ListLeaderboardQuery.cs` + handler. Paged list of users with `LeaderboardOptIn = true` (current user always included regardless), ordered by current points desc.
  - `LeaderboardController` adds `GET /api/leaderboard`.
- **Acceptance test:** user A opted out → not visible to user B; user A toggles opt-in → visible.
- **Guidance:** Backend.

### BT-031 — HealthKit ingest stub

- **Requirements:** L2-023.
- **Slice:**
  - `Forge.Application/Abstractions/IHealthKitIngest.cs`.
  - `Forge.Infrastructure/Deferred/LoggingHealthKitIngest.cs`.
  - `Forge.Application/HealthKit/IngestHealthKitSampleCommand.cs` + validator + handler that delegates to `IHealthKitIngest` and returns 202.
  - `HealthKitController` adds `POST /api/healthkit/ingest`.
  - Documented in `backend.md` under "deferred integrations" — full integration deferred until a later FI1 / BI follow-up.
- **Acceptance test:** post a payload → `202`; one structured log line `healthkit.ingest.deferred` written.
- **Guidance:** General (deferred integration as named no-op).

### BT-032 — Notification dispatcher

- **Requirements:** L2-025, L2-026.
- **Slice:**
  - `Forge.Application/Abstractions/INotificationSender.cs`.
  - `Forge.Infrastructure/Deferred/LoggingNotificationSender.cs`.
  - `Forge.Api/HostedServices/NotificationDispatcherHostedService.cs` running on a `PeriodicTimer(TimeSpan.FromMinutes(1))`. Each tick scans users whose configured morning window or kitchen-closed boundary is within a small upcoming window, and calls `INotificationSender` accordingly.
  - Wired in `Program.cs` via `builder.Services.AddHostedService<NotificationDispatcherHostedService>()`.
- **Acceptance test:** seed a user with morning window starting 1 minute from "now" (use `IClock`); within 2 ticks, one log entry is emitted.
- **Guidance:** Backend, General (no real transport in MVP).

### BT-033 — Security headers middleware (CSP, HSTS, nosniff, referrer)

- **Requirements:** L2-052, L2-049.
- **Slice:**
  - `Forge.Api/Middleware/SecurityHeadersMiddleware.cs`. Adds `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; frame-ancestors 'none'`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`. In production, also adds `Strict-Transport-Security: max-age=31536000; includeSubDomains`.
  - Registered in `Program.cs` before `UseAuthentication`.
- **Acceptance test:** any 200 response carries the documented headers.
- **Guidance:** General (security baseline).

### BT-034 — Structured logging + secret redaction filter

- **Requirements:** L2-043, L2-051.
- **Slice:**
  - Configure `Microsoft.Extensions.Logging` with the JSON console formatter and the explicit fields `timestamp`, `level`, `message`, `traceId`, `userId`.
  - Add a destructuring filter that redacts log-property keys named `password`, `accessToken`, `refreshToken`, `passwordResetToken`, `Authorization`.
  - Add a request-logging middleware that captures `traceId` from `Activity.Current` and pushes `userId` from `ICurrentUser` into the log scope.
- **Acceptance test:** trigger a sign-in with an obviously-distinctive password; grep log output for that string — zero matches. Trigger any handled request; log line is valid JSON containing the five required fields.
- **Guidance:** Observability.

### BT-035 — Readiness endpoint

- **Requirements:** L2-044.
- **Slice:**
  - `HealthController` already exposes `GET /health` (liveness). Add `GET /health/ready` that runs `await _db.Database.CanConnectAsync(...)` and returns `200` healthy / `503` unhealthy.
- **Acceptance test:** `GET /health/ready` against a running app → `200 { status: "Healthy" }`. With a broken connection string → `503`.
- **Guidance:** Observability.

### BT-036 — CI dependency vulnerability scan

- **Requirements:** L2-053.
- **Slice:**
  - `.github/workflows/ci.yml` — jobs that run on every push:
    - `dotnet restore && dotnet build && dotnet test` for backend.
    - `npm ci && npm audit --audit-level=high` for frontend (and `package-lock.json` checked in).
    - `dotnet list package --vulnerable --include-transitive` and fail if any high/critical advisory matches.
- **Acceptance test:** intentionally pin a known-vulnerable dependency in a draft PR → CI fails. Remove → CI passes.
- **Guidance:** General (CI / dep hygiene).

## Sequencing summary

| Phase | Tasks                       | Comment |
|-------|-----------------------------|---------|
| BI1.1 | BT-001..BT-006, BT-006a, BT-007..BT-013 | Foundation + auth completeness + sessions CRUD. BT-001 must land first; BT-006a (`IClock`) lands before any time-sensitive slice. |
| BI1.2 | BT-014..BT-020              | Profile / weight / windows. Depends on BI1.1 schema. |
| BI1.3 | BT-022..BT-029              | Gamification end-to-end. BT-022 ships the migration + base scorer; BT-023..BT-024 extend the scorer; BT-025 ties refunds; BT-026..BT-029 expose read paths. (BT-021 was merged into BT-022 — its slot remains for cross-reference.) |
| BI1.4 | BT-030..BT-036              | Leaderboard, deferred integrations, hardening. Can land in any order once the BI1.3 schema is in place. BT-036 (CI) is independent and can land at any point. |

Each task is small enough that one BI1 loop iteration can: write the acceptance test (Playwright POM or backend integration), implement the slice, run the rubric eval, fix on find, and mark the task done.
