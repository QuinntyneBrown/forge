# BT2 — Evaluate backend task list

## Pass 1 - findings

Walked the BT2 explicit checks against `./docs/plans/backend-tasks.md` at commit `6036596`. The 36 tasks span four phases (BI1.1..BI1.4) and were each inspected against criteria 1–6.

### Coverage and conformance — confirmed

- **Forbidden abstractions** (criterion 3) — `grep -E "IRepository|IUnitOfWork|class.*Repository"` over `backend-tasks.md` returns zero matches. No task introduces a repository or unit-of-work. ✅
- **Acceptance test named** (criterion 4) — every task entry has an "Acceptance test" line. Backend-only slices (auth, sessions, profile, gamification, leaderboard, HealthKit ingest, notifications, security headers, logging, readiness, CI dep scan) cite a backend integration test (e.g. `RefreshTokenAcceptanceTest`); UI-driven slices in BI1 will ship Playwright POM tests in FT1/FI1 paired with the corresponding backend slice. ✅
- **Guidance rules named** (criterion 5) — every task ends with a `Guidance:` line citing the relevant Implementation Guidance section(s) (Backend / Validation / Authentication / General / Observability). ✅
- **Sizing** (criterion 6) — no task spans more than one feature folder + one controller + one migration; all are sized for one BI1 loop iteration to write the test, implement, evaluate, and mark done. ✅
- **Vertical-slice shape** (criterion 1) — most tasks pair controller endpoint + command/query + validator + handler + entity changes + acceptance test. The slices that look "horizontal" (BT-008 audit logger wired into many handlers, BT-024 streak multiplier extending the scorer) are still end-to-end because they ship a behavior change with a behaviorally-asserted test. Acceptable.

### Mock-back-pressure check

Backend-tasks doesn't need to cover mocked screens directly (that's FT1's job), but the L2s named in each task were cross-checked against the L2 traceability matrix in `L2.md`. Every backend-relevant L2 (L2-001..L2-023, L2-025..L2-027, L2-031..L2-044, L2-049..L2-054 — excluding the frontend-only ones) is named by at least one task. ✅

The following blocking findings were found:

### Finding 1 — BT-021 is a migration-only "scaffolding" task with no end-to-end value (criterion 2)

BT-021 ("Migration `AddPointsAndRewards`") adds three tables + seed rows but ships no user-visible behavior. Its acceptance test is a database-shape check (`query RewardCatalogItem — seeded rows present`), not a behavioral test against a handler or endpoint. Per criterion 2 ("no task is scaffolding only with no end-to-end value"), this is a finding.

The fix is to fold BT-021 into BT-022 ("Base points scoring + IPointsScorer"), which is the first slice that exercises the new tables. The merged BT-021/022 slice ships the migration, the seed, the `ScoringConstants` file, the `IPointsScorer` interface and implementation, and the `CreateSessionCommandHandler` integration. Its acceptance test "create a 22-min session → ledger row +44 (Base — 22 min logged) appears" reads end to end against the new schema.

(BT-001 "Initial EF migration replacing `EnsureCreated`" is also migration-heavy but is genuinely an enabling slice — every subsequent task assumes `MigrateAsync` works against the captured schema, and replacing `EnsureCreated` is a meaningful runtime-behavior change. The acceptance test is mechanical but justified. Leaving BT-001 as-is.)

**Fix:** merge BT-021 into BT-022. Renumber the rest of BI1.3 (BT-021..BT-029 collapses by one) — *but per BT2 ID hygiene rules, do not renumber*; instead drop BT-021 from the list and have BT-022 absorb it. Keep BT-021's slot as a stub paragraph noting "merged into BT-022" so existing references aren't broken.

### Finding 2 — `IClock` abstraction is referenced once but not standardized as its own slice

BT-007 mentions "use a test seam — inject `IClock` if necessary" but the abstraction is never enumerated as a slice and is not referenced from BT-022 (streak / morning-bonus computation needs "now in user TZ"), BT-024 (streak resets on missed day — needs deterministic "yesterday"), BT-029 (dashboard "today" calorie/minute totals), BT-032 (notification dispatcher tick), or BT-018 (window validation). Without a single standardized clock, each slice will reach for `DateTimeOffset.UtcNow` directly, making each acceptance test brittle and tightly coupled to wall time.

**Fix:** add a dedicated slice `BT-007a — IClock abstraction` between BT-006 and BT-007 (or label it BT-007 and bump the throttle slice to BT-008-style numbering — but again, prefer adding the new slice ID to avoid renumbering existing tasks). The new slice ships:

- `Forge.Application/Abstractions/IClock.cs` — `DateTimeOffset UtcNow { get; }`.
- `Forge.Infrastructure/SystemClock.cs` — returns `DateTimeOffset.UtcNow`.
- DI registration as `Singleton`.

Acceptance test: a backend integration test that swaps `IClock` for a fixture and asserts a handler reads the fixture's "now". Then update the BT-007, BT-022, BT-024, BT-029, BT-032, BT-018 task entries to mention `IClock` as the time source they use.

### Non-blocking observations

- BT-008 (audit logger) wires into seven handlers (register, sign-in, sign-out, refresh, password reset confirm, account delete, plus the throttle path). The slice is honest about the touch-many-files shape; flagging it here so the BI1 loop budgets a slightly larger PR for that task.
- BT-024 (streak multiplier) requires reading "distinct calendar days of prior sessions" — the SQL approach (`GROUP BY CAST(StartedAt AT TIME ZONE 'X' AS DATE)`) needs the user's IANA time zone. The slice should explicitly use `IClock` + `User.TimeZoneId` rather than UTC date boundaries; otherwise streaks reset at the wrong time for non-UTC users. Adding to the slice description in the same edit as Finding 2.
- BT-036 (CI dep scan) introduces a new file (`.github/workflows/ci.yml`) that affects merge gating. Calling out so the implementer knows to confirm the user's GitHub Actions billing / repo settings before adding the workflow file.
- The plan already enumerates the four no-op deferred services (`LoggingPasswordResetEmailSender`, `LoggingNotificationSender`, `LoggingHealthKitIngest`, `EmptyLeaderboardSource`). `EmptyLeaderboardSource` is named by BP1 but BT-030 says the leaderboard is read directly off `Users` rather than through `ILeaderboardSource`. That's fine — `ILeaderboardSource` was a plan placeholder for a future federation case. Leaving it out of the task list is correct; updating BP1 to drop it would be a BP2 follow-up not a BT2 finding.

## Pass 2 - findings

Re-walked the BT2 explicit checks against `./docs/plans/backend-tasks.md` after applying the two Pass 1 fixes.

- **Finding 1 (BT-021 scaffolding only)** — resolved. BT-021's slot is now an empty cross-reference paragraph noting it was merged into BT-022. BT-022 is now titled "Migration `AddPointsAndRewards` + base points scoring + `IPointsScorer`" and ships the migration, the seed, the scoring constants, the abstraction, the implementation, and the `CreateSessionCommandHandler` integration in one slice. Its acceptance test now asserts both the seeded `RewardCatalogItem` rows and the `+44 (Base — 22 min logged)` ledger entry produced by the new scorer — end-to-end behavioral coverage. The sequencing summary table reflects the change ("BI1.3 | BT-022..BT-029 ... BT-021 merged into BT-022").
- **Finding 2 (`IClock` standardization)** — resolved. New slice `BT-006a — IClock abstraction` lives between BT-006 and BT-007 and ships `IClock` + `SystemClock` + DI registration with a `ClockSeamAcceptanceTest`. The slices that read time (BT-007 throttle window, BT-024 streak multiplier in user TZ) now explicitly cite `IClock` / `IClock.TodayInTimeZone(...)` rather than `DateTimeOffset.UtcNow`. The non-blocking observation about BT-024 evaluating streak boundaries in the user's time zone is folded into the same edit. The sequencing summary table now lists `BT-006a` in BI1.1.

Re-checks across all six BT2 criteria:
1. Vertical-slice shape — every remaining task ships behavior + acceptance test. BT-006a is small but lands an abstraction that downstream slices reference; its acceptance test asserts a behavior swap. ✅
2. No scaffolding-only tasks — BT-021 retired; BT-022 absorbed it; everything else was already vertical. ✅
3. No forbidden abstractions — unchanged. ✅
4. Every task names its acceptance test — BT-006a names `ClockSeamAcceptanceTest`; the rest unchanged. ✅
5. Every task names guidance — BT-006a cites Backend + General. ✅
6. Sizing — BT-006a is smaller than the average slice; BT-022 is now larger than average but still bounded (one migration, one DI registration, one extension to `CreateSessionCommandHandler`, one acceptance test). Within the "few loop iterations" budget. ✅

Pass 2 produces zero blocking findings. BT2 is complete.
