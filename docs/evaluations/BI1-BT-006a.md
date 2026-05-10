# BI1 — BT-006a — `IClock` abstraction

## Pass 1 - findings

Walked the Implementation Evaluation Rubric (criteria 1–10) against the BT-006a implementation. Scope per BI1: Backend, Validation, Authentication, Testing, and General sections.

### Mechanical checks

- One-type-per-file: `IClock`, `SystemClock`, `FakeClock`. Each declares a single top-level type matching its filename. ✅
- `grep -E "TODO|FIXME|XXX|HACK|NotImplementedException"` over `backend/src` — zero matches.
- `grep -E "IRepository|IUnitOfWork|class.*Repository"` over `backend/src` — zero matches.
- `dotnet build` — 5 projects, 0 errors / 0 warnings.
- `dotnet test tests/Forge.Acceptance` — `4 Passed` (BT-001 + BT-002 + BT-003 + BT-006a).
- Frontend `npx playwright test` — `4 passed (16.9s)`. No regression.

### Structural checks (criteria 1, 6 — Guidance adherence + SOLID/CQS shape)

- `IClock` lives in `Forge.Application/Abstractions/`. Interface exposes `DateTimeOffset UtcNow { get; }` and `DateOnly TodayInTimeZone(string ianaTimeZoneId)`. The latter signature anticipates BT-024 streak-multiplier and BT-029 dashboard "today" usage where calendar boundaries depend on the user's local time zone.
- `SystemClock : IClock` in `Forge.Infrastructure/`. `UtcNow` returns `DateTimeOffset.UtcNow`. `TodayInTimeZone` resolves the IANA id via `TimeZoneInfo.FindSystemTimeZoneById` and converts. Registered as `Singleton` in `Forge.Infrastructure/DependencyInjection.cs` (matches the `IPasswordHasher` / `IJwtTokenIssuer` lifetime — pure functions, no per-request state).
- Migrated time-stamping consumers to `IClock`:
  - `RegisterCommandHandler` injects `IClock` and writes `User.CreatedAt = _clock.UtcNow` (was `DateTimeOffset.UtcNow`).
  - `RefreshTokenStore` injects `IClock`; every `var now = DateTimeOffset.UtcNow` in `IssueAsync` / `ConsumeAsync` / `RevokeFamilyAsync` / `RevokeByPresentedTokenAsync` was rewritten to `var now = _clock.UtcNow`.
- Subsequent slices that need the seam (BT-007 throttle window, BT-022/023/024 scoring, BT-029 dashboard "today", BT-032 notification dispatcher) inject `IClock` instead of reaching for wall time.

### Acceptance test (criterion 8 — ATDD evidence)

`tests/Forge.Acceptance/Auth/ClockSeamAcceptanceTest.cs` was authored before the `RegisterCommandHandler` migration. Header: `// Traces to: BT-006a (IClock abstraction)`. The test:

1. Provisions a per-test LocalDB and applies migrations.
2. Boots `WebApplicationFactory<Program>` with the canonical `ConfigureAppConfiguration` + `ConfigureTestServices` override pattern, **plus** swaps the registered `IClock` for a `FakeClock` pinned to `2026-05-10T05:12:00+00:00`.
3. Posts to `/api/auth/register` against the per-test DB.
4. Reads the persisted `User` row from a fresh `AppDbContext` and asserts `user.CreatedAt == PinnedNow` (exact equality, not approximate).

`tests/Forge.Acceptance/Auth/FakeClock.cs` ships in the test project (not in `Forge.Infrastructure`) so the production binary doesn't carry test-only types. The fake exposes `Advance(TimeSpan)` so future time-window tests (BT-007 throttle, BT-024 streak) can move time deterministically without sleeping.

### Build / run clean (criterion 10)

- `dotnet build` — green.
- `dotnet test tests/Forge.Acceptance` — `4 Passed`.
- `dotnet run --project src/Forge.Api` — boots cleanly; `/health` returns 200.
- Frontend `playwright test` against the live updated backend — `4 passed`. No regression on the existing flows (sign-in, sign-up, auth-guard redirect, sign-out).

### Non-blocking observations

- The slice migrates two existing call sites (`RegisterCommandHandler.CreatedAt`, `RefreshTokenStore` timestamps) to demonstrate the seam end to end. `SignInCommandHandler` and other future handlers will be migrated in their own slices when they introduce time-dependent behavior — keeping each commit small and tightly scoped.
- `SystemClock.TodayInTimeZone` throws `TimeZoneNotFoundException` for an unknown IANA id; that's acceptable because user time-zone strings come from server-side validation (BT-015 `UpdateProfileCommand` will validate against `TimeZoneInfo.GetSystemTimeZones()` before persisting).
- Registering `IClock` as `Singleton` is safe because the implementation is stateless. If a future slice needs a per-request clock (e.g., for testing-via-headers in development), the registration switches to `Scoped` — a one-line change.
- Tests run sequentially because each provisions and drops its own LocalDB. The 4-test suite completes in ~9 seconds locally, which is acceptable. If this grows past ~20s, the BT2 non-blocking observation about a shared `ForgeAcceptanceFactory` base class becomes worth acting on.

Pass 1 produces zero blocking findings. BT-006a is complete.
