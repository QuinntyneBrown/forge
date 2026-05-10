# TP3 — Pass 3

**Date:** 2026-05-10
**Operator:** claude@LAPTOP-C0RT0N4M

## Environment

Same as previous passes. `accessibility.spec.ts` for the five
anonymous routes: `5 passed (22.7s)`.

## Findings

### B-006 (low) — `dashboard.page.ts` still calls `meApi.getMe()` after sign-in

After the FT-024 dashboard rewrite, the page still calls
`IMeService.getMe()` on init even though `AuthStateService.snapshot()`
already carries the email + role. The redundant call costs one extra
round trip per dashboard navigation. Skipping it (or at least making
it best-effort behind a `tryHydrate` style cache) would shave ~30 ms
off the first dashboard render. Stylistic; not blocking.

### B-007 (info) — `RewardsCatalogComponent` queries dashboard summary just for the balance

The rewards page renders both `<forge-tier-card>` (which loads the
dashboard summary) and `<forge-rewards-catalog>` (which also loads the
dashboard summary). Two parallel `GET /api/dashboard` calls fire on
page load. A shared signal-based service (or `shareReplay()` on the
observable) would dedupe. Tracking.

## Outcome

- No new findings affect correctness; both items concern duplicated
  HTTP load on the rewards / dashboard pages.
- No fixes shipped this pass.
- Proceeding to pass 4.
