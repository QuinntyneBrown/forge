# Backend implementation plan

This plan turns the approved L1/L2 requirements into a concrete .NET implementation that extends the MB1 MVP. Every item below cites the L2(s) it satisfies and the Implementation Guidance section that constrains it. The MVP shape (Clean Architecture, MediatR + FluentValidation, `IAppDbContext`, JWT bearer, bcrypt password hashing) is the reference — nothing in this plan deviates from it.

## 1. Project layout

Inherits the MVP layout. No additional projects.

```
backend/
  Forge.sln
  global.json
  Directory.Build.props
  src/
    Forge.Domain/         (no deps)
    Forge.Application/    (depends: Forge.Domain)
    Forge.Infrastructure/ (depends: Forge.Application)
    Forge.Api/            (depends: Forge.Application, Forge.Infrastructure)
  tests/
    Forge.Acceptance/     (added in BT1 — Playwright POM acceptance tests)
```

Constraints (Implementation Guidance — Backend, General):
- One type per `.cs` file, file name matches type name (L2-054).
- Inward dependency direction enforced via project references.
- `TreatWarningsAsErrors=true` in `Directory.Build.props`.

## 2. Domain model

Each entity lives in its own file under `Forge.Domain/`.

| Type                      | Purpose                                                                   | L2s |
|---------------------------|---------------------------------------------------------------------------|-----|
| `User`                    | Account record. `Id`, `Email`, `FirstName`, `LastName`, `PasswordHash`, `Role` (enum-backed string), `Units` (Imperial/Metric), `TimeZoneId`, `DailyActiveCaloriesTarget`, `DailyWorkoutMinutesTarget`, `MonthlyWeightGoalLb`, `MorningWindowStart`, `MorningWindowEnd`, `KitchenClosedStart`, `KitchenClosedEnd`, `KitchenNudgeEnabled`, `MorningReminderEnabled`, `LeaderboardOptIn`, `IsDeleted`, `DeletedAt`, `CreatedAt`. | L2-001, L2-005, L2-006, L2-011, L2-014, L2-016, L2-017, L2-026, L2-027, L2-050 |
| `EquipmentType` (enum)    | `Treadmill=1, IndoorBike=2, BenchPress=3, Elliptical=4`.                  | L2-010 |
| `WorkoutSession`          | `Id`, `UserId`, `Equipment`, `StartedAt`, `DurationMinutes`, `DistanceMiles?`, `AvgHeartRateBpm?`, `ActiveCalories`, `Notes?`, `Source` (`Manual`/`AppleWatch`), `CreatedAt`. | L2-007, L2-008, L2-009, L2-023 |
| `WeightEntry`             | `Id`, `UserId`, `WeightLb`, `RecordedAt`.                                 | L2-015 |
| `PointsLedger`            | `Id`, `UserId`, `SessionId?`, `Delta` (signed int), `Reason` (enum), `Multiplier` (decimal, default 1.00), `CreatedAt`. | L2-018, L2-019, L2-020, L2-021 |
| `PointsLedgerReason` (enum) | `Base=1, MorningBonus=2, StreakMultiplier=3, RewardRedemption=4, Refund=5`. | L2-018, L2-019, L2-020, L2-021 |
| `RewardCatalogItem`       | `Id`, `Name`, `Description`, `CostPoints`, `IsActive`. Static seeded.     | L2-021 |
| `RewardRedemption`        | `Id`, `UserId`, `RewardCatalogItemId`, `CostPoints`, `RedeemedAt`.        | L2-021 |
| `RefreshToken`            | `Id`, `UserId`, `TokenHash`, `FamilyId`, `IssuedAt`, `ExpiresAt`, `ConsumedAt?`, `RevokedAt?`. | L2-002, L2-003, L2-033 |
| `SignInAttempt`           | `Id`, `Email`, `IpAddress`, `UserAgent`, `Success`, `OccurredAt`.         | L2-034, L2-035 |
| `AuditLog`                | `Id`, `UserId?`, `Event` (string), `IpAddress?`, `UserAgent?`, `OccurredAt`, `PayloadJson?` (no PII). | L2-035 |
| `PasswordResetToken`      | `Id`, `UserId`, `TokenHash`, `IssuedAt`, `ExpiresAt`, `ConsumedAt?`.       | L2-004 |

Domain types are POCOs — no EF or HTTP concerns. Aggregates are kept thin; computed values (streak length, today's totals, current tier) are derived in the Application layer rather than persisted.

Constraints (Implementation Guidance — Backend, General): no DataAnnotations on domain types; one type per file (L2-054); domain references nothing.

## 3. EF Core / DbContext / migrations

`AppDbContext : DbContext, IAppDbContext` lives in `Forge.Infrastructure`. The `IAppDbContext` interface in `Forge.Application/Abstractions` exposes one `DbSet<T>` per persisted aggregate above plus `SaveChangesAsync(CancellationToken)`. Handlers depend on `IAppDbContext` only — no repository, no unit-of-work (Implementation Guidance — Backend).

Configuration:
- All entity configuration in `OnModelCreating` (one `.HasMaxLength`, `.HasPrecision`, `.HasIndex` block per entity). No separate `IEntityTypeConfiguration<T>` files for the MVP-scope schema; if the configuration grows past ~30 lines per entity it is split into one configuration file per entity (still one type per file).
- Unique index on `Users.Email` (L2-001), unique on `Users.IsDeleted=false ∧ Email` filtered to allow re-registration after deletion → MVP keeps the simpler unique-on-`Email` and account deletion anonymizes the email field (L2-006, L2-050).
- Composite index `WorkoutSessions(UserId, StartedAt)` (L2-008 list/filter performance).
- `RefreshTokens.TokenHash` indexed; `FamilyId` indexed (L2-033 family revocation).
- `SignInAttempts(Email, OccurredAt)` indexed (L2-034 lookup window).

Migration sequencing — one migration per implementation slice, named after the slice:

| # | Migration                       | Adds                                          | Slice      |
|---|---------------------------------|-----------------------------------------------|------------|
| 1 | `Initial_AuthAndSessions`       | `Users`, `WorkoutSessions`, `RefreshTokens`, `SignInAttempts`, `AuditLog`, `PasswordResetTokens` | BI1.1 (auth) |
| 2 | `AddProfileAndWeight`           | `Users` profile columns + `WeightEntries`     | BI1.2 (profile) |
| 3 | `AddPointsAndRewards`           | `PointsLedger`, `RewardCatalogItem`, `RewardRedemption` (seeded catalog) | BI1.3 (gamification) |

The MB1 MVP committed `Database.EnsureCreated()`. **First action of BI1.1**: replace `EnsureCreated` with `Database.MigrateAsync()` and add the initial migration that captures the MVP schema as-is, then evolve from there.

## 4. Application layer — command, query, and validator inventory

Grouped by feature folder. Each command/query is a separate file. Each command has a colocated `*Validator.cs` (L2-039). Handlers depend on `IAppDbContext`, `ICurrentUser`, and feature-specific abstractions only.

### 4.1 Auth (`Forge.Application/Auth/`)

| Command/Query                | Validator?              | Handler reads/writes                                   | L2s |
|------------------------------|-------------------------|--------------------------------------------------------|-----|
| `RegisterCommand`            | `RegisterCommandValidator` (email, name, password policy ≥12 + complexity) | Hash with `IPasswordHasher`, insert `User`, issue access + refresh tokens, audit `register.success`. | L2-001, L2-031 |
| `SignInCommand`              | `SignInCommandValidator` (email, password non-empty) | Throttle via `ISignInThrottle.Check(email, ip)` (L2-034). Verify hash. On failure: append `SignInAttempt(success=false)`, audit `sign-in.failure`, throw `InvalidCredentialsException`. On success: append `SignInAttempt(success=true)`, audit `sign-in.success`, issue tokens. | L2-002, L2-034, L2-035 |
| `RefreshTokenCommand`        | `RefreshTokenCommandValidator` (non-empty token) | Hash incoming token, look up by hash. If consumed → revoke entire family, throw 401. Else mark consumed, issue new pair, audit `token.refresh`. | L2-033, L2-035 |
| `SignOutCommand`             | `SignOutCommandValidator` (refresh token) | Revoke the supplied refresh token + its family, audit `sign-out`. | L2-003, L2-035 |
| `RequestPasswordResetCommand` | `RequestPasswordResetCommandValidator` (email) | Always returns 202. If user exists, generate single-use token (hash stored), send via `IPasswordResetEmailSender` (no-op `LoggingPasswordResetEmailSender` for MVP). | L2-004 |
| `ConfirmPasswordResetCommand` | `ConfirmPasswordResetCommandValidator` (token + new password meeting policy) | Verify token + not consumed + not expired. Hash + replace `PasswordHash`. Mark token consumed. Revoke all refresh tokens for the user. Audit `password-reset.success`. | L2-004, L2-031, L2-035 |
| `DeleteAccountCommand`       | `DeleteAccountCommandValidator` (no payload — uses `ICurrentUser`) | Soft-delete user, anonymize `FirstName`/`LastName`/`Email` to a sentinel, revoke all refresh tokens, schedule background nightly job to verify cleanup (L2-050). Audit `account.delete`. | L2-006, L2-050 |
| `GetCurrentUserQuery`        | n/a                     | Project to `CurrentUserDto` (id, email, role, profile fields).                | L2-005 |

### 4.2 Profile (`Forge.Application/Profile/`)

| Command/Query                | Validator?                                | Handler reads/writes                                   | L2s |
|------------------------------|-------------------------------------------|--------------------------------------------------------|-----|
| `UpdateProfileCommand`       | `UpdateProfileCommandValidator` (lengths, time-zone IANA id, target ranges) | Update `User` profile columns; if email changed, enforce uniqueness or throw 409. | L2-005 |
| `RecordCurrentWeightCommand` | `RecordCurrentWeightCommandValidator` (>0, ≤1500 lb) | Insert `WeightEntry`. | L2-015 |
| `SetMonthlyWeightGoalCommand`| `SetMonthlyWeightGoalCommandValidator` (1..30 lb / month) | Update `User.MonthlyWeightGoalLb`. | L2-014 |
| `UpdateMorningWindowCommand` | `UpdateMorningWindowCommandValidator`     | Update `User.MorningWindowStart`/`End`.               | L2-016 |
| `UpdateKitchenWindowCommand` | `UpdateKitchenWindowCommandValidator`     | Update `User.KitchenClosedStart`/`End`/`KitchenNudgeEnabled`. | L2-017, L2-026 |
| `SetLeaderboardOptInCommand` | `SetLeaderboardOptInCommandValidator`     | Update `User.LeaderboardOptIn`.                        | L2-027 |

### 4.3 Sessions (`Forge.Application/Sessions/`)

| Command/Query                  | Validator?                                       | Handler reads/writes                                                          | L2s |
|--------------------------------|--------------------------------------------------|------------------------------------------------------------------------------|-----|
| `CreateSessionCommand`         | `CreateSessionCommandValidator` (existing — extend distance rule per equipment) | Insert `WorkoutSession(Source=Manual)`. Trigger `IPointsScorer.Score(session)` to append `PointsLedger` rows. | L2-007, L2-018, L2-019, L2-020 |
| `UpdateSessionCommand`         | `UpdateSessionCommandValidator`                  | Update fields, recompute points (delete prior ledger rows for session, re-score). | L2-009 |
| `DuplicateSessionCommand`      | `DuplicateSessionCommandValidator` (id)          | Insert clone with `StartedAt = today`, score new ledger rows.                 | L2-009 |
| `DeleteSessionCommand`         | `DeleteSessionCommandValidator` (id)             | Append refund `PointsLedger` rows, delete session.                            | L2-009 |
| `ListSessionsQuery`            | n/a                                              | Filtered list (equipment, date range, notes substring), paged.                | L2-008 |
| `GetSessionByIdQuery` (existing) | n/a                                            | Project to `SessionDto`.                                                      | L2-007 |

### 4.4 Equipment (`Forge.Application/Equipment/`)

| Command/Query                | Validator? | Handler reads/writes                              | L2s |
|------------------------------|-----------|--------------------------------------------------|-----|
| `ListEquipmentQuery`         | n/a       | Returns the four enum values + display names.    | L2-010 |

### 4.5 Dashboard (`Forge.Application/Dashboard/`)

| Command/Query              | Validator? | Handler reads/writes                                                                   | L2s |
|----------------------------|------------|----------------------------------------------------------------------------------------|-----|
| `GetDashboardSummaryQuery` | n/a        | Pure read aggregating: today's active calories, today's minutes, current streak, current points balance, current tier, next reward within reach, month-to-date weight delta vs goal. Single SQL round-trip via `IAppDbContext` aggregations. | L2-011, L2-012, L2-013, L2-014, L2-022 |

### 4.6 Rewards (`Forge.Application/Rewards/`)

| Command/Query              | Validator?                          | Handler reads/writes                                              | L2s |
|----------------------------|-------------------------------------|------------------------------------------------------------------|-----|
| `ListRewardsQuery`         | n/a                                 | Active catalog items.                                              | L2-021 |
| `RedeemRewardCommand`      | `RedeemRewardCommandValidator`      | Compute current balance (sum ledger). If insufficient → 400 `INSUFFICIENT_POINTS`. Else insert `RewardRedemption` + ledger row `(Delta=-cost, Reason=RewardRedemption)`. | L2-021 |
| `GetCurrentTierQuery`      | n/a                                 | Lifetime points → tier (deterministic threshold table).            | L2-022 |

### 4.7 HealthKit ingest (`Forge.Application/HealthKit/`) — deferred no-op for MVP

| Command/Query                  | Validator?                            | Handler reads/writes                                                            | L2s |
|--------------------------------|---------------------------------------|--------------------------------------------------------------------------------|-----|
| `IngestHealthKitSampleCommand` | `IngestHealthKitSampleCommandValidator`| Stub: validate shape, write a structured log line `healthkit.ingest.deferred` via `ILoggingHealthKitIngest` (no-op service named accordingly). Returns 202. The real ingest is deferred — the L2-023 acceptance criteria are validated against a stub adapter until the integration slice. | L2-023 |

### 4.8 Notifications (`Forge.Application/Notifications/`) — deferred no-op for MVP

| Command/Query                | Validator? | Handler reads/writes                                                | L2s |
|------------------------------|-----------|--------------------------------------------------------------------|-----|
| `SendMorningReminderCommand` | n/a       | `ILoggingNotificationSender` writes a structured log row.          | L2-025 |
| `SendKitchenNudgeCommand`    | n/a       | Same.                                                              | L2-026 |

These are exercised by a single hosted service (`NotificationDispatcherHostedService`) running on a `PeriodicTimer`; in MVP it logs the intended action. The hosted service itself is real — only the underlying transport is deferred.

## 5. Controller surface

One controller per feature slice. Every action either sends a `Command` or a `Query` through `IMediator`. Controllers shape HTTP only.

| Controller          | Routes                                                                                                      | Auth                       | L2s |
|---------------------|-------------------------------------------------------------------------------------------------------------|----------------------------|-----|
| `AuthController`    | `POST /api/auth/register`, `/sign-in`, `/refresh`, `/sign-out`, `/password-reset/request`, `/password-reset/confirm` | anonymous (sign-out requires bearer) | L2-001..L2-004, L2-033 |
| `MeController`      | `GET /api/me`, `DELETE /api/me`                                                                              | `[Authorize]`              | L2-005, L2-006 |
| `ProfileController` | `PUT /api/profile`, `POST /api/profile/weight`, `PUT /api/profile/weight-goal`, `PUT /api/profile/morning-window`, `PUT /api/profile/kitchen-window`, `PUT /api/profile/leaderboard-opt-in` | `[Authorize]`              | L2-005, L2-014..L2-017, L2-027 |
| `SessionsController`| `GET /api/sessions`, `GET /api/sessions/{id}`, `POST /api/sessions`, `PUT /api/sessions/{id}`, `POST /api/sessions/{id}/duplicate`, `DELETE /api/sessions/{id}` | `[Authorize]`              | L2-007..L2-009 |
| `EquipmentController` | `GET /api/equipment`                                                                                       | `[Authorize]`              | L2-010 |
| `DashboardController` | `GET /api/dashboard`                                                                                       | `[Authorize]`              | L2-011..L2-013, L2-022 |
| `RewardsController` | `GET /api/rewards`, `POST /api/rewards/{id}/redeem`, `GET /api/tier`                                         | `[Authorize]`              | L2-021, L2-022 |
| `HealthKitController` | `POST /api/healthkit/ingest`                                                                              | `[Authorize]`              | L2-023 |
| `AdminController`   | `GET /api/admin/users` (sample admin-only endpoint)                                                          | `[Authorize(Roles="Admin")]` | L2-037, L2-038 |
| `HealthController` (existing) | `GET /health`                                                                                       | anonymous                  | L2-044 |

Anonymous endpoints are explicitly listed (`AllowAnonymous` per action where the controller is `[Authorize]`d) — every other endpoint requires a bearer token (L2-038).

## 6. Auth flow end-to-end

1. **Register** → bcrypt hash (work factor 12, L2-031) → insert `User` (default role `User`, L2-037) → issue JWT (HS256, claims `iss`, `aud`, `sub`, `email`, `role`, `jti`, `nbf`, `iat`, `exp`, L2-032) and refresh token (opaque, hashed at rest, L2-033). Audit `register.success`.
2. **Sign-in** → check throttle (`ISignInThrottle`: 5 failures per email per 15-minute window → 429 lockout for 15 minutes, L2-034) → verify hash → record `SignInAttempt` and `AuditLog` row → issue tokens.
3. **Refresh** → look up by hashed token. Reuse → revoke entire family (L2-033). Else mark consumed, issue new pair.
4. **Sign-out** → revoke supplied refresh token + family. Access token expires naturally (≤60 min).
5. **Password reset** → request always 202 (no enumeration, L2-004) → token via `ILoggingPasswordResetEmailSender` (no-op email until SES/SendGrid arrives) → confirm validates token + revokes all refresh tokens.
6. **Account deletion** → anonymize PII columns synchronously, revoke tokens, audit, schedule nightly verifier (L2-050).

`ISignInThrottle` lives in `Forge.Application/Abstractions`; the implementation in `Forge.Infrastructure` reads `SignInAttempts` over the rolling window. Pure SQL count, no in-memory cache for MVP.

`IAuditLogger` writes `AuditLog` rows; called from handlers, never from controllers.

`IRefreshTokenStore` exposes `Issue`, `Consume`, `Revoke`, `RevokeFamily`. Stored hash is `SHA-256` of the opaque token (the raw token is returned once at issue time and never persisted).

## 7. Cross-cutting

- **Validation pipeline** — existing `ValidationBehavior<TRequest, TResponse>` continues to handle every command/query (L2-039). The exception-handling middleware maps `ValidationException` → 400 with `application/problem+json` (L2-040, fixed in MB2).
- **Authorization** — `[Authorize]` at the controller level; anonymous actions explicitly opt out via `[AllowAnonymous]`. Admin-only endpoints use `[Authorize(Roles="Admin")]` (L2-037, L2-038).
- **Logging** — `Microsoft.Extensions.Logging` with structured JSON (Serilog or built-in JSON formatter). Every log record carries `timestamp`, `level`, `message`, `traceId`, `userId` (L2-043). Secrets redacted via a `LoggingFilter` that strips fields named `password`, `accessToken`, `refreshToken`, `passwordResetToken` from any payload before write (L2-051).
- **Audit log** — separate channel from operational logs; rows persisted to `AuditLog` table, never to stdout. Trace id captured for cross-reference.
- **Health endpoint** — existing `GET /health` (L2-044). Add a second `GET /health/ready` that pings the DB and any required external transports (none for MVP).
- **CSP and security headers** — middleware in `Forge.Api/Middleware/SecurityHeadersMiddleware.cs` adds `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; object-src 'none'; frame-ancestors 'none'`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`, `Strict-Transport-Security` (production only) (L2-052).
- **HTTPS redirect + HSTS** — already wired in MVP for production via `UseHttpsRedirection`. HSTS enabled in non-development (L2-049).
- **Performance budgets** — every handler avoids N+1 queries; list endpoints use `.AsNoTracking()` and project directly to DTO (L2-041). Targets validated by load-test harness in BT1.

## 8. Deferred integrations and their no-op replacements

Each deferred integration is implemented as a clearly-named no-op service in `Forge.Infrastructure/Deferred/` that logs the intended action and returns success. The interface lives in `Forge.Application/Abstractions`. The replacement at production-rollout time is a registration swap, not a code change in handlers.

| Deferred integration                | Interface                          | MVP no-op implementation                         | L2 |
|-------------------------------------|------------------------------------|-------------------------------------------------|----|
| Transactional email (password reset) | `IPasswordResetEmailSender`       | `LoggingPasswordResetEmailSender`               | L2-004 |
| Push notifications (morning reminder, kitchen nudge) | `INotificationSender` | `LoggingNotificationSender`                  | L2-025, L2-026 |
| Apple HealthKit ingest              | `IHealthKitIngest`                 | `LoggingHealthKitIngest` (accepts payload, logs) | L2-023 |
| Friend leaderboard data source      | `ILeaderboardSource`               | `EmptyLeaderboardSource` (returns no rows)       | L2-027 |

Every no-op service is named `Logging…` or `Empty…` so it is unambiguous in DI registration and stack traces. Each is documented in `docs/runbooks/backend.md` under "deferred integrations" once introduced.

## 9. Migration / slice sequencing for BI1

BI1 ships in three commits, each ATDD-driven (Playwright POM acceptance tests authored in BT1 are the gating criterion):

1. **BI1.1 — Auth + Sessions** (L2-001..L2-004, L2-007..L2-010, L2-031..L2-038). Migration: `Initial_AuthAndSessions`. Hosted services: none yet. Sample-slice work from MB1 already covers register/sign-in/create-session — extend to refresh, sign-out, password reset, account deletion, full session CRUD, equipment query, RBAC.
2. **BI1.2 — Profile + Weight + Windows** (L2-005, L2-014..L2-017, L2-026, L2-027). Migration: `AddProfileAndWeight`. Adds profile controller surface and the kitchen / morning window settings.
3. **BI1.3 — Gamification** (L2-011..L2-013, L2-018..L2-022). Migration: `AddPointsAndRewards`. Adds points scorer, ledger, dashboard summary, rewards catalog + redemption, tier query. Rewards catalog seeded via migration `HasData`.

After the three slices, `BI1.4` (operational hardening) adds `SecurityHeadersMiddleware`, `IAuditLogger`, structured JSON logging, secret redaction filter, and the `/health/ready` endpoint.

## 10. Verification matrix

Every L2 in scope for the backend is satisfied by at least one item above. Frontend-only L2s (L2-024 error UI, L2-028 empty UI, L2-029 error UI, L2-030 responsive, L2-042 dashboard FCP/TTI, L2-045..L2-048 a11y) are not in this plan. Cross-cutting:

| L2     | Backend artifact in this plan                                    |
|--------|------------------------------------------------------------------|
| L2-001 | `RegisterCommand` (§4.1) + `Users` schema (§2)                   |
| L2-002 | `SignInCommand` (§4.1)                                           |
| L2-003 | `SignOutCommand` (§4.1)                                          |
| L2-004 | `RequestPasswordResetCommand` + `ConfirmPasswordResetCommand` (§4.1) + `LoggingPasswordResetEmailSender` (§8) |
| L2-005 | `MeController` + `ProfileController` (§5), `UpdateProfileCommand` (§4.2) |
| L2-006 | `DeleteAccountCommand` (§4.1)                                    |
| L2-007 | `CreateSessionCommand` (§4.3)                                    |
| L2-008 | `ListSessionsQuery` + `WorkoutSessions(UserId, StartedAt)` index (§3) |
| L2-009 | `Update`/`Duplicate`/`DeleteSessionCommand` (§4.3)                |
| L2-010 | `EquipmentType` enum + `ListEquipmentQuery` (§4.4)                |
| L2-011 | `User.DailyActiveCaloriesTarget` + `GetDashboardSummaryQuery` (§4.5) |
| L2-012 | `GetDashboardSummaryQuery` (§4.5)                                |
| L2-013 | `GetDashboardSummaryQuery` (§4.5)                                |
| L2-014 | `SetMonthlyWeightGoalCommand` (§4.2)                             |
| L2-015 | `RecordCurrentWeightCommand` + `WeightEntries` (§2)              |
| L2-016 | `UpdateMorningWindowCommand` + `User.MorningWindowStart/End` (§2)|
| L2-017 | `UpdateKitchenWindowCommand` (§4.2)                              |
| L2-018..L2-020 | `IPointsScorer` invoked from session command handlers, persists `PointsLedger` rows (§4.3, §2) |
| L2-021 | `ListRewardsQuery` + `RedeemRewardCommand` (§4.6)                |
| L2-022 | `GetCurrentTierQuery` + tier threshold table (§4.6)              |
| L2-023 | `IHealthKitIngest` no-op (§8)                                    |
| L2-025 | `INotificationSender` no-op + dispatcher hosted service (§4.8)   |
| L2-026 | `INotificationSender` + `UpdateKitchenWindowCommand` (§4.2)      |
| L2-027 | `SetLeaderboardOptInCommand` + `ILeaderboardSource` no-op (§8)   |
| L2-031 | `BCryptPasswordHasher` (work factor 12)                          |
| L2-032 | JWT bearer middleware (§6)                                       |
| L2-033 | `RefreshToken` entity + `IRefreshTokenStore` (§6)                |
| L2-034 | `ISignInThrottle` + `SignInAttempts` (§6)                        |
| L2-035 | `IAuditLogger` + `AuditLog` (§6, §7)                              |
| L2-036 | No PKCE / no external IdP code paths exist (§6)                  |
| L2-037 | `User.Role` + `[Authorize(Roles="Admin")]` on `AdminController` (§5) |
| L2-038 | `[Authorize]` controller defaults; explicit `AllowAnonymous` per action (§5) |
| L2-039 | Validators colocated with each command (§4)                      |
| L2-040 | `ExceptionHandlingMiddleware` (existing, fixed in MB2)           |
| L2-041 | `.AsNoTracking()` + projections + indexes (§3, §7)               |
| L2-043 | Structured JSON logging via `Microsoft.Extensions.Logging` (§7)  |
| L2-044 | `HealthController` (existing) + `/health/ready` (§7)              |
| L2-049 | HTTPS redirect + HSTS in production (§7); password hashes only (L2-031) |
| L2-050 | `DeleteAccountCommand` anonymization (§4.1)                      |
| L2-051 | `LoggingFilter` redaction (§7)                                   |
| L2-052 | `SecurityHeadersMiddleware` CSP (§7)                              |
| L2-053 | CI workflow runs `dotnet list package --vulnerable --include-transitive` and fails on high/critical (BT1 will add the workflow file). |
| L2-054 | Project layout (§1) + one-type-per-file enforced throughout      |
