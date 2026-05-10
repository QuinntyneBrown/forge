# TP3 — Pass 2

**Date:** 2026-05-10
**Operator:** claude@LAPTOP-C0RT0N4M

## Environment

Same as pass 1. Static review against the test plan + automated suites
(SQLEXPRESS not installed locally; backend acceptance suite assertions
remain authoritative once the DB is reachable).

`accessibility.spec.ts` re-run for the five anonymous routes — green:
`5 passed (22.4s)`.

## Findings

### B-004 (low) — Bundle size warning above the 500 KB budget

`ng build forge` warns the initial bundle is ~798 KB (~298 KB over the
500 KB budget). The budget is the Angular default; the app is well
within real-world thresholds for a Material 3 SPA. Either raise the
budget in `frontend/projects/forge/angular.json` to 800 KB or decide to
chase the saving with lazy-loaded routes for `/workouts/*` and
`/rewards`. Tracking; deferring to a perf-tuning pass.

### B-005 (info) — `hideDistance` computed re-renders on every equipment-control valueChanges

`WorkoutDetailFormComponent` subscribes to `equipment.valueChanges` and
mirrors into a signal. This is fine but slightly redundant — Angular's
`toSignal()` from `@angular/core/rxjs-interop` would express the same
intent more idiomatically. Stylistic; not blocking.

## Outcome

- B-001..B-005 all classified info / low; none change behaviour
  observable to the user.
- No fixes shipped this pass.
- Proceeding to pass 3.
