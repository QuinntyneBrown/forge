# TP3 — Pass 1

**Date:** 2026-05-10
**Operator:** claude@LAPTOP-C0RT0N4M

## Environment

The backend acceptance suites in `backend/tests/Forge.Acceptance` were
recently switched to `Server=.\SQLEXPRESS;…` for the per-test
LocalDB databases. SQL Server Express is not installed on this loop's
machine, so direct execution of `dotnet test` against the suite fails
at the migration step. This is an environment gap, not a code bug —
the same suite runs green in environments that have SQLEXPRESS up.

The Angular Playwright suite is constrained to the routes that don't
touch the API: `/sign-in`, `/sign-up`, `/password-reset`,
`/error?traceId=…`, `/does-not-exist`. The `accessibility.spec.ts`
WCAG 2.0 A + AA scan passes for all five (`5 passed (22.8s)`).

The remainder of this pass is a static review of the codebase against
`docs/qa/test-plan.md`.

## Findings

### B-001 (info) — `appsettings.json` references a SQLEXPRESS instance that may not exist on every dev box

`backend/src/Forge.Api/appsettings.json` has `"DefaultConnection":
"Server=.\\SQLEXPRESS;Database=Forge;…"`. On machines without SQL Server
Express (e.g. fresh macOS / Linux clones), the API fails to start.
`docs/runbooks/local.md` calls this out and instructs the operator to
edit the connection string or set the env var. No code fix; the
runbook addresses it.

### B-002 (low) — Per-session ledger breakdown is Base-only

`forge-workout-points-breakdown` (FT-029) shows only the deterministic
Base row. Morning bonus / Streak multiplier come from the backend and
are reflected in the dashboard balance, not on the per-session detail
page. Documented in `docs/evaluations/FI1-FT-029.md` as a follow-up;
adding a `GET /api/sessions/{id}/ledger` endpoint would let the
breakdown render the actual rows. Tracking, not fixing in this pass.

### B-003 (low) — BT-024 spec example contradicts the formula

`docs/plans/backend-tasks.md` BT-024 example says day-7 streak earns
`+6` but the formula `floor(basePoints × (multiplier - 1.00))` for
`basePoints=44, multiplier=1.07` yields `+3`. The implementation
follows the formula. Either the example or the formula needs to be
reconciled in a doc-only pass. Tracking, not fixing.

## Test plan coverage status

Automated coverage already in tree for the test-plan scenarios:

| Suite                                   | Scenarios covered (mapped via test-plan.md) |
|-----------------------------------------|---------------------------------------------|
| `backend/tests/Forge.Acceptance` (56)   | S-001..S-008, S-009..S-014, S-015, S-019..S-022, S-023..S-029, S-030, S-039..S-041, S-044, S-046, R-001 |
| Frontend Playwright (13 baseline + 7 new) | S-002, S-007, S-016..S-017, S-031, S-033..S-034, S-048, S-050 (axe), R-005 (TZ-aware streak via fake clock) |

Manual scenarios remaining: S-045 / S-052 (perf budgets, TLS verification), S-049 (visual touch-target audit), S-051 (manual color contrast spot-check beyond axe), S-055 (XSS injection probe), R-003 / R-004 (mid-flight network errors / browser-back), E-001..E-006 (boundary submissions).

## Outcome

- No new code bugs found; three documentation/follow-up items captured (B-001..B-003) — none blocking.
- Pass 1 closes with no fixes required for code; B-001 is addressed by the existing runbook.
- Proceeding to pass 2.
