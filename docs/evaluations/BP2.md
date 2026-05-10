# BP2 — Evaluate backend plan

## Pass 1 - findings

Walked the BP2 explicit checks plus the Backend / Validation / Authentication / General sections of Implementation Guidance and the L1 / L2 spec at commit `2e21d50` against `./docs/plans/backend.md`.

### Coverage and conformance — confirmed

- **Clean Architecture layering planned.** §1 lays out four projects (`Forge.Domain` → `Forge.Application` → `Forge.Infrastructure` → `Forge.Api`) with the inward dependency direction enforced via project references. ✅
- **CQS via MediatR planned.** Every feature folder in §4 enumerates commands and queries; controllers in §5 only call `IMediator.Send(...)`. ✅
- **No repository / unit-of-work in the plan.** §3 explicitly says "Handlers depend on `IAppDbContext` only — no repository, no unit-of-work". A grep of the plan confirms no `IRepository`, `IUnitOfWork`, or repository-pattern terminology appears. ✅
- **FluentValidation per command planned.** §4 tables list a `*Validator` for every command (queries are `n/a` per the rule that queries don't carry side effects). The validation pipeline behavior is reused from the MVP. ✅
- **`IAppDbContext` abstraction planned.** §3 names the interface and shows `AppDbContext : DbContext, IAppDbContext`. ✅
- **Auth flow planned with hashing + JWT issuance + JWT validation + RBAC.** §6 walks register → sign-in → refresh → sign-out → password reset → account deletion with explicit references to bcrypt work factor 12, HS256 JWT claims (`iss`, `aud`, `sub`, `email`, `role`, `jti`, `nbf`, `iat`, `exp`), refresh-token rotation with family revocation, sign-in throttle (5 failures / 15 min), audit logging. RBAC is in §5 (`[Authorize(Roles="Admin")]` on `AdminController`, default `User` role on registration). ✅
- **SQL Server.** §3 references `Microsoft.EntityFrameworkCore.SqlServer`. ✅
- **One-type-per-file convention assumed.** §1 closes with the constraint and §2 notes "each entity lives in its own file". ✅
- **Deferred integrations enumerated as no-op logging services.** §8 lists four (`IPasswordResetEmailSender`, `INotificationSender`, `IHealthKitIngest`, `ILeaderboardSource`) with `Logging…` / `Empty…` named implementations. ✅
- **No speculative abstractions.** Plan declines to introduce `IRepository`, mediator-decorator chains beyond the existing `ValidationBehavior`, AutoMapper / Mapster, generic CRUD bases, or feature-flag scaffolding. ✅

### L2 verification matrix sanity-check

§10 enumerates every backend-scoped L2 (L2-001..L2-023, L2-025..L2-027, L2-031..L2-044, L2-049..L2-054). Frontend-only L2s (L2-024 error UI, L2-028 empty UI, L2-029 error UI, L2-030 responsive, L2-042 dashboard FCP/TTI, L2-045..L2-048 a11y) are explicitly out of scope and called out as such. The matrix terminates each L2 in a concrete plan artifact. ✅

The following blocking findings were found:

### Finding 1 — Scoring constants for L2-018 / L2-019 / L2-020 / L2-022 are unspecified

The plan introduces `IPointsScorer.Score(session)` in §4.3 and a `GetCurrentTierQuery` with a "deterministic threshold table" in §4.6, but never enumerates the actual constants. Two implementation problems follow: the implementer would have to invent values that may not match the L2 acceptance criteria, and the BT1 acceptance tests cannot assert exact numbers.

The L2 acceptance criteria require:

- L2-018: `2 points per minute` of logged workout (a 22-minute session ⇒ +44).
- L2-019: `+25 morning bonus` when `StartedAt` falls inside the morning window.
- L2-020: streak multiplier `≥ 1.00`, rounded to two decimals; 7 consecutive days at `×1.07` adds `+6` for a 22-minute session (i.e. `floor(44 * 0.07)`).
- L2-022: five tiers (`Bronze`, `Silver`, `Forged Iron`, `Gold`, `Platinum`); `Forged Iron` lives at `5,000 lifetime points`. The remaining four thresholds need values.

**Fix:** add a "Scoring constants and tier thresholds" subsection under §4.6 (or as a new §11) that fixes:

- `BasePointsPerMinute = 2`
- `MorningBonusPoints = 25`
- `StreakMultiplierFormula = 1.00 + min(0.50, 0.01 × streakDays)` (caps at ×1.50)
- `TierThresholds = { Bronze: 0, Silver: 1000, ForgedIron: 5000, Gold: 15000, Platinum: 40000 }` (lifetime points)

Tier thresholds and multiplier cap are reasonable values consistent with the visible behavior in the mocks (Tier 3 at ~5k matches the `profile.html` mock); the plan should commit to specific numbers so BT1 can assert them.

### Finding 2 — CORS policy is missing from the plan

The Angular workspace at `frontend/` will call `Forge.Api` from a different origin during development (typically `http://localhost:4200` → `https://localhost:5001`). Without an explicit CORS policy registered in `Program.cs`, every fetch from the Angular dev server will fail at preflight. The plan never names CORS — neither §5 (controllers) nor §7 (cross-cutting) mention it.

**Fix:** add a CORS subsection to §7 specifying:

- A named policy `"web"` registered in `Program.cs`.
- Allowed origins read from configuration (`Cors:AllowedOrigins`), defaulting in Development to `http://localhost:4200` and `https://localhost:4200`.
- `AllowAnyHeader`, `AllowAnyMethod`, `AllowCredentials` (the JWT bearer flow does not strictly need credentials, but cookie-based refresh paths added later may).
- `app.UseCors("web")` placed before `UseAuthentication` / `UseAuthorization`.
- Production deployment overrides `Cors:AllowedOrigins` per environment.

### Finding 3 — No read path for the leaderboard

§4.2 plans `SetLeaderboardOptInCommand` and §8 plans an `ILeaderboardSource` no-op service, but no query or controller exposes leaderboard data. L2-027 acceptance criteria require user B to load a leaderboard and observe whether user A's row is present — the plan currently has no `ListLeaderboardQuery` and no controller endpoint to read from. Without a read path, neither acceptance criterion is testable end-to-end.

**Fix:** add to §4.2 a `ListLeaderboardQuery` (returns paged `LeaderboardEntryDto[]` filtered by `User.LeaderboardOptIn = true`, sorted by current points desc), and add `GET /api/leaderboard` to the controller surface in §5 under a new `LeaderboardController` (or co-located on `RewardsController` — pick one in the fix). Update the §10 matrix row for L2-027.

### Non-blocking observations

- The plan defers the dependency-scanning CI workflow file to BT1. That's fine because BT1 is the task that introduces test infrastructure, and dep scanning runs alongside tests in CI. Calling it out here so BP2 reviewers don't double-flag it as a gap when it lands later.
- §6 fixes the access-token expiration at "≤60 min" via configuration. Reasonable for MVP. Refresh-token expiration is left unspecified — the L2-033 acceptance criteria don't require a specific value, but BI1.1 should pick one (e.g., 14 days) and document it in the runbook.
- The plan never names the JWT short-form `role` claim (the issuer writes `ClaimTypes.Role` which serializes to the long URI). MB2 already noted this as non-blocking; reiterating here so BI1.1 considers adding a parallel short-form claim if the frontend RBAC consumption is cleaner that way.

## Pass 2 - findings

Re-walked the BP2 explicit checks against the updated `./docs/plans/backend.md` after applying the three Pass 1 fixes.

- **Finding 1 (Scoring constants and tier thresholds)** — resolved. New §8.1 enumerates `BasePointsPerMinute = 2`, `MorningBonusPoints = 25`, the streak-multiplier formula `min(1.50, 1.00 + 0.01 × consecutiveDays)` with the local-time-zone reset rule, and the five tier thresholds (`Bronze 0`, `Silver 1,000`, `Forged Iron 5,000`, `Gold 15,000`, `Platinum 40,000`). Constants are pinned to `Forge.Application/Gamification/ScoringConstants.cs` so handlers and acceptance tests reference one source.
- **Finding 2 (CORS policy)** — resolved. §7 now adds a named policy `"web"` registered from `Cors:AllowedOrigins` (defaults to localhost:4200 in Development), `AllowAnyHeader/Method`, `AllowCredentials`, applied via `app.UseCors("web")` placed before `UseAuthentication` / `UseAuthorization`.
- **Finding 3 (Leaderboard read path)** — resolved. §4.2 adds `ListLeaderboardQuery` (paged, opt-in only, ordered by current points desc; current user's row always included). §5 adds a `LeaderboardController` exposing `GET /api/leaderboard` under `[Authorize]`. §10 matrix row for L2-027 updated.

Re-checks across the BP2 explicit checks: Clean Architecture / CQS / no repository / FluentValidation / `IAppDbContext` / Auth flow / SQL Server / one-type-per-file / no-op deferred integrations / no speculative abstractions all still pass after the additions. The new `LeaderboardController` follows the same shape as every other controller (calls `IMediator.Send`, no business logic). The new `ListLeaderboardQuery` is a query (no validator required by rule).

Pass 2 produces zero blocking findings. BP2 is complete.
