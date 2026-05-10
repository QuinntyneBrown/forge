# FT2 — Evaluate frontend task list

## Pass 1 - findings

Walked the FT2 explicit checks against `./docs/plans/frontend-tasks.md` at commit `cacdd49`.

### Coverage and conformance — confirmed

- **Library boundaries** (criterion: "no task crosses library boundaries inconsistently with the guidance"). Every task names the libraries it touches. Walking each:
  - FT-001..FT-011 — `components` only.
  - FT-012, FT-013, FT-027..FT-032, FT-033 — touch `api` (service + DTOs) + `domain` (component) + `forge` (page) in the documented direction.
  - FT-014, FT-015, FT-024, FT-034 — `forge` only (interceptors / pages / state).
  - FT-016, FT-018, FT-022, FT-023, FT-025, FT-026 — `api` only.
  - FT-017 — `components` (composition).
  - FT-035 — tests only.
  - No task imports `api` or `domain` from `components`; no task imports from `forge` in any library; no service or backend-talking model lives outside `api`. ✅
- **Acceptance test named** (criterion 4) — every task has an "Acceptance test" line. Some tasks defer to a downstream consumer's spec (see Finding 1) but the spec is named in every entry. ✅
- **Guidance rules named** (criterion 5) — every task ends with a `Guidance:` line citing the relevant Implementation Guidance section(s) (Frontend / Authentication / Validation / General). ✅
- **No forbidden abstractions** — `grep` of `frontend-tasks.md` for `IRepository`, `UnitOfWork`, single-file component patterns (`template:`, `styles:`), DataAnnotations — zero matches. ✅
- **Sizing** (criterion 6) — most tasks fit one loop iteration. The two larger ones (FT-027 workouts list + chips + page + spec; FT-031 ProfileService with six endpoints + ProfileForm + page + spec) are explicitly OK because the workflow allows "a small number of loop iterations". ✅

### Library placement (CRITICAL)

Walked the assignment of every component / service / model in the task list against the FP1 plan and the rubric:

- **Reusable presentation components** — FT-001..FT-011 + FT-017 all assign their target components to `components`. None land in `domain`, `forge`, or `api`. ✅
- **Backend-facing services + their models** — FT-012, FT-013 (extending `IAuthService`), FT-016 (`MeService`), FT-018 (`DashboardService`), FT-022 (`RewardsService`), FT-023 (`LeaderboardService`), FT-025 (`SessionsService`), FT-026 (`EquipmentService`), FT-030 (extending `RewardsService`), FT-031 (`ProfileService`), FT-033 (`HealthKitService`) — every one lands its service + `*.service.contract.ts` + DTO models under `projects/api/src/lib/`. None appear in `components`, `domain`, or `forge`. ✅
- **Components that consume the api layer** — FT-012 (`SignUpFormComponent`), FT-013 (`PasswordResetRequest/ConfirmFormComponent`), FT-019..FT-023 (the five dashboard cards), FT-027 (`WorkoutListComponent`), FT-028 (`WorkoutDetailFormComponent`), FT-029 (`WorkoutPointsBreakdownComponent`), FT-030 (`RewardsCatalogComponent`), FT-031 (`ProfileFormComponent`), FT-033 (`SyncErrorPanelComponent`) — every one lands in `domain`. None appear in `components` or `forge`. ✅

### Mock coverage

`./docs/mocks/`'s eleven screens map to:

- `index.html` — n/a (mock-only nav, intentionally no production page).
- `sign-in.html` — existing `SignInPage` (MF1) + FT-014 + FT-015.
- `sign-up.html` — FT-012.
- `password-reset.html` — FT-013.
- `dashboard.html` — FT-018..FT-024.
- `workouts.html` — FT-027.
- `workout-detail.html` — FT-028 (create variant) + FT-029 (edit variant).
- `rewards.html` — FT-030.
- `profile.html` — FT-031 + FT-032.
- `empty-state.html` — FT-034 (404) + FT-027 zero-rows variant.
- `error-state.html` — FT-033.

Every mock is implemented by at least one task. ✅

### L2 coverage

Cross-checked the L2 IDs claimed by each task against the FP1 §12 verification matrix. Every UI L2 (L2-001..L2-030, L2-036, L2-042, L2-045..L2-048, L2-052) is named by at least one task. L2-052 (XSS / CSP) is met implicitly through Angular's default HTML encoding plus the lint rule against `[innerHTML]`; the convention is documented in FP1 §1 / §3 but does not have a dedicated FT-* task. Acceptable — it's a coding convention, not a feature.

The following blocking finding was found:

### Finding 1 — Several FI1.0 wrapper tasks (and one service task) defer their acceptance test to a downstream consumer without specifying that the wrapper ships *with* that consumer (criterion 2: no scaffolding-only tasks)

Ten tasks have an "Acceptance test" line that reads "verified inside `<other-spec>` (FT-NNN). No standalone spec required" or equivalent:

- FT-003 IconButton → verified inside FT-027 (workout list menu) and FT-031 (password reveal)
- FT-005 Checkbox → verified inside FT-015 sign-in-remember-me
- FT-006 Switch → verified inside FT-031 profile
- FT-007 Chip → verified inside FT-027 workouts-list
- FT-008 ProgressRing → verified inside FT-024 dashboard and FT-030 rewards-redeem
- FT-009 Badge → verified inside existing sign-in.spec.ts (health-badge dependency)
- FT-010 EmptyState → verified inside FT-034 empty-state spec
- FT-011 ErrorBanner → verified inside FT-033 error-state spec
- FT-016 MeService → verified inside FT-031 profile and FT-032 account-deletion

If an implementer picks up FT-006 (`SwitchComponent`) in isolation, they ship a wrapper that no test exercises and no user touches. That literally is "scaffolding only with no end-to-end value" per criterion 2 — the wrapper has no acceptance evidence on its own.

The current task list also leaves it ambiguous whether the implementer should land FT-006 first as a standalone PR (which then sits unused until FT-031 lands later) or land FT-006 *together with* FT-031 in one PR (which is what the "verified inside" phrasing implies but never spells out).

**Fix:** add an explicit convention in the file's preamble (right after the existing "Conventions:" list):

> **"Verified inside" tasks are landed in the same commit as their downstream consumer.** When a task's "Acceptance test" line says "verified inside `<other-spec>`", the wrapper / service is not implemented as a standalone PR — it ships with its first consumer task in one PR, and the consumer's spec is the gating acceptance test. Specifically: FT-003, FT-005, FT-006, FT-007, FT-008, FT-009, FT-010, FT-011, and FT-016 each ride along with the FT-* listed in their acceptance line. This satisfies criterion 2 by treating the wrapper + first-consumer pair as a single end-to-end slice.

That paragraph turns the ten ambiguous entries into ten unambiguous "land-together" pairings. No task IDs change; no slice content moves; the rubric criterion is satisfied because every PR ships a full vertical slice.

For each "verified inside" task, also add a single line at the end of its slice description explicitly naming its consumer task and noting "ships in the same commit as that task". Concretely:

- FT-003 → "Ships in the same commit as FT-027 (the first consumer)."
- FT-005 → "Ships in the same commit as FT-015."
- FT-006 → "Ships in the same commit as FT-031."
- FT-007 → "Ships in the same commit as FT-027."
- FT-008 → "Ships in the same commit as FT-019 (DailyRingCardComponent — first consumer)."
- FT-009 → "Ships in the same commit as FT-001 / existing sign-in.spec.ts coverage of `<forge-health-badge>` — domain composition swap, no separate PR."
- FT-010 → "Ships in the same commit as FT-027 (zero-row variant) or FT-034, whichever lands first."
- FT-011 → "Ships in the same commit as FT-033."
- FT-016 → "Ships in the same commit as FT-031."

### Non-blocking observations

- FT-001 (CardComponent → mat-card swap) does have a real standalone acceptance test (existing `sign-in.spec.ts` keeps passing because the dashboard's `<forge-card title="Server status">` continues to render). It's the cleanest "land in isolation" wrapper task; it doesn't fall under Finding 1.
- FT-002 (Button → mat-flat-button) has its own `tests/components/button.smoke.spec.ts` per the slice description, which is a synthetic harness route. That's a borderline acceptance test — it asserts DOM shape rather than user-visible behavior — but the consumer specs (FT-012 sign-up, FT-013 password-reset, FT-027 workouts list buttons) will exercise it for real once those tasks land. Acceptable.
- FT-004 (Field → mat-form-field) similarly has a "verified by FT-012/FT-013" pattern in its description. Folding it under the same convention as Finding 1 (ships with FT-012) would be tidier; calling out as a non-blocker because the FP1 plan treats `Field` as the central form primitive and FT-012 + FT-013 + FT-028 + FT-031 all need it on day one. Implementer can land FT-004 alongside FT-012 as the first consumer.
- FT-014 (refreshInterceptor + authGuard) and FT-015 (Remember-me persistence) overlap in the L2-002 / L2-033 acceptance criteria. They're separable but reviewers should expect some shared edits to `AuthStateService`. Sizing remains within one iteration each.
- FT-022 (TierCard) introduces `IRewardsService` with only `getCurrentTier()` and FT-030 extends it with `listRewards` + `redeem`. Splitting the service across two tasks is fine because each ships its own consumer; calling out so reviewers don't flag it as a scattered service definition.

## Pass 2 - findings

Re-walked the FT2 explicit checks against `./docs/plans/frontend-tasks.md` after applying the Pass 1 fix.

- **Finding 1 (scaffolding-only ambiguity)** — resolved. The "Conventions" preamble now contains a fifth bullet that explicitly defines the *"verified inside" → ships-with-consumer* convention by name and lists the nine affected task IDs (FT-003, FT-005, FT-006, FT-007, FT-008, FT-009, FT-010, FT-011, FT-016). Each of those tasks now ends with a "**Ships with:**" line naming the specific FT-* it lands in the same commit as:
  - FT-003 → FT-027 (first consumer).
  - FT-005 → FT-015.
  - FT-006 → FT-031.
  - FT-007 → FT-027.
  - FT-008 → FT-019 (first consumer).
  - FT-009 → composition swap on existing `<forge-health-badge>` covered by current `sign-in.spec.ts`, in one commit.
  - FT-010 → FT-027 zero-row variant or FT-034, whichever lands first.
  - FT-011 → FT-033.
  - FT-016 → FT-031.
  Treating each pair as one PR turns nine ambiguous "scaffolding only" entries into nine unambiguous vertical slices, each gated by an end-to-end Playwright POM spec from the consumer task. Criterion 2 satisfied.

Re-checks across all FT2 criteria:
1. Vertical-slice shape — every task either ships its own end-to-end slice or is explicitly paired with a consumer that does. ✅
2. No scaffolding-only tasks — see above. ✅
3. Library boundaries — unchanged. ✅
4. Acceptance test named — every task names a Playwright POM spec, either its own or its consumer's. ✅
5. Guidance rules named — unchanged. ✅
6. Sizing — unchanged; the "Ships with" pairings don't make any single PR materially larger because the wrapper tasks were already small. ✅
7. CRITICAL library placement — unchanged. ✅

Pass 2 produces zero blocking findings. FT2 is complete.
