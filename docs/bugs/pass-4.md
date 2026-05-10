# TP3 — Pass 4

**Date:** 2026-05-10
**Operator:** claude@LAPTOP-C0RT0N4M

## Environment

Same as previous passes. `accessibility.spec.ts` for the five
anonymous routes: `5 passed (22.8s)`.

## Findings

### B-008 (info) — `error.page` is reachable without a session

By design (per FT-033 / `app.routes.ts`), `/error` has no `authGuard`
so the panel can render when auth has already failed. The trade-off
is that a malicious link like `/error?traceId=ATTACK` trivially
renders the page. There's no PII or destructive action on the page,
so impact is informational at most. Documented as intentional in the
runbook.

### B-009 (info) — Soft-deleted users still appear in the leaderboard query for themselves

`ListLeaderboardQueryHandler` filters `u => !u.IsDeleted && (u.LeaderboardOptIn || u.Id == callerId)`. After delete (FT-032), the caller is rerouted to /sign-in and the JWT is gone, so the leaderboard query never runs for them — but the filter is still defensive. No issue.

## Outcome

- No findings of any severity beyond informational notes.
- No fixes shipped this pass.
- Proceeding to pass 5.
