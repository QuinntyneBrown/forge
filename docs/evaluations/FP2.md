# FP2 — Evaluate frontend plan

## Pass 1 - findings

Walked the FP2 explicit checks against `./docs/plans/frontend.md` at commit `0aebfd6`.

### Coverage and conformance — confirmed

- **Library structure with correct dependency direction.** §1 declares `components → nothing; domain → api + components; forge → api + domain`. The component / service inventories in §3, §4 and §2 respect that direction (every service is in `api`, every reusable presentation component is in `components`, every backend-consuming UI component is in `domain`). ✅
- **Every planned service has `*.service.contract.ts`.** §2 table has a "Contract file" column populated for all ten services (`auth.service.contract.ts`, `health.service.contract.ts`, `me.service.contract.ts`, `profile.service.contract.ts`, `sessions.service.contract.ts`, `equipment.service.contract.ts`, `dashboard.service.contract.ts`, `rewards.service.contract.ts`, `leaderboard.service.contract.ts`, `healthkit.service.contract.ts`). Each contract file exports an interface and an `InjectionToken`. ✅
- **Design tokens specified.** §6 enumerates color roles, type scale (display / headline / title / body / label tiers), elevation tiers, 4-pixel spacing scale, shape scale, and breakpoints (XS / SM / MD / LG / XL) — every token as a CSS custom property. ✅
- **BEM naming assumed.** §1 closes with the BEM constraint and §3 / §4 follow naming conventions consistent with MF1 (`card`, `card__header`, `sign-in-form__field`, etc.). ✅
- **Per-file split assumed.** §1 declares the `.ts`/`.html`/`.scss` triple constraint. ✅
- **Playwright POM tests for important flows.** §8 lists 14 specs (sign-in, sign-up, password-reset, account-deletion, dashboard, workouts-list, workout-create, workout-delete-refund, rewards-redeem, profile, responsive, accessibility, error-state, empty-state) plus page objects under `frontend/tests/pom/`. Every test header carries `// Traces to: <L2-IDs>`. ✅
- **Local username/password auth flow planned.** §7 walks AuthStateService, authInterceptor, the new refreshInterceptor + authGuard, and explicitly excludes PKCE / external IdPs (L2-036). ✅
- **Mock-to-screen mapping exhaustive.** §10 maps every mock under `./docs/mocks/` (`index`, `sign-in`, `sign-up`, `password-reset`, `dashboard`, `workouts`, `workout-detail`, `rewards`, `profile`, `empty-state`, `error-state`) to a page or page-state. ✅

### Library placement (CRITICAL)

- **Every reusable presentation component is assigned to `components`.** §3 places all 14 (Card, Button, IconButton, Field, Checkbox, Switch, Chip, ProgressRing, Badge, EmptyState, ErrorBanner, AppShell, BottomNav, NavRail) in `components`. ✅
- **Every model and backend-facing service is in `api`.** §2 places all ten services with their DTO models under `projects/api/src/lib/`. ✅
- **Every component that needs api is in `domain`.** §4 places all 16 (SignInForm, SignUpForm, PasswordResetRequestForm, PasswordResetConfirmForm, HealthBadge, DailyRingCard, StreakCard, WeightProgressCard, TierCard, LeaderboardCard, WorkoutList, WorkoutDetailForm, WorkoutPointsBreakdown, RewardsCatalog, ProfileForm, SyncErrorPanel) in `domain`. ✅

### L2 coverage

§12 lists every UI L2 (L2-001..L2-030, L2-036, L2-042, L2-045..L2-048, L2-052) with a concrete artifact. Backend-only L2s are explicitly named as out of scope. ✅

The following blocking findings were found:

### Finding 1 — Plan ships hand-rolled presentation components instead of Angular Material wrappers (Implementation Guidance — Frontend)

The Frontend section of Implementation Guidance is unambiguous: "**Angular Material** components for every component — buttons, headers, inputs, etc. Visual language is Material 3." MF1's runbook acknowledged that the MVP punted on Material and noted that FI1 would swap to `<mat-form-field>` / `<button mat-flat-button>` etc. as the design library matures.

§3 of the plan re-lists the hand-rolled MVP components (`ButtonComponent`, `IconButtonComponent`, `FieldComponent`, `CheckboxComponent`, `SwitchComponent`, `ChipComponent`, `ProgressRingComponent`, `BadgeComponent`, etc.) without specifying that each one wraps an Angular Material component. As written, FI1 implementers would re-implement pure-CSS controls and miss the requirement.

**Fix:** add a row to each `components/` entry naming the Angular Material component it wraps. Concretely:

- `ButtonComponent` → wraps `<button mat-flat-button>` / `mat-stroked-button` / `mat-button` selected by `[variant]`.
- `IconButtonComponent` → wraps `<button mat-icon-button>` (with Material Symbols glyphs).
- `FieldComponent` → wraps `<mat-form-field appearance="outline">` containing a projected `<input matInput>` / `<textarea matInput>` / `<mat-select>`. Floating label, hint, error slots come from Material directly.
- `CheckboxComponent` → wraps `<mat-checkbox>`.
- `SwitchComponent` → wraps `<mat-slide-toggle>`.
- `ChipComponent` → wraps `<mat-chip>` / `<mat-chip-listbox>` for filter chips.
- `BadgeComponent` → wraps `<mat-chip>` styled as a status pill (or a `[matBadge]` host depending on the surface — name one).
- `ProgressRingComponent` → wraps `<mat-progress-spinner mode="determinate">`. The dashboard ring's exact 1500-kcal layout sits in `domain` (`DailyRingCardComponent`) which composes `ProgressRingComponent`.
- `BottomNavComponent` / `NavRailComponent` → wrap `<mat-tab-nav-bar>` and `<mat-nav-list>` respectively (or `<mat-bottom-sheet>` patterns — pick one in the fix and document).
- `AppShellComponent` → wraps `<mat-toolbar>` for the top bar, the chosen nav primitive, and projects `<router-outlet>`.
- `EmptyStateComponent` and `ErrorBannerComponent` are pure layout — they don't have a one-to-one Material counterpart, so they remain hand-rolled and the plan should call this out so reviewers don't flag them.
- `CardComponent` (existing) — note in the plan that FI1 swaps it to wrap `<mat-card>` while keeping the existing `<forge-card title="...">` selector + content-projection contract so domain components don't need to change.

Also add a §6 note: design tokens drive `mat.theme(...)` already wired in `styles.scss` (existing in MF1), so Material components automatically render in the Forge Fit palette.

### Finding 2 — Plan §7 forbids `localStorage` / `sessionStorage` but L2-002 requires refresh-token persistence across browser restart

§7 of the plan reads: *"Storage policy: tokens live in memory only — no `localStorage`, no `sessionStorage`. Hard refresh of the browser ends the session."* L2-002 acceptance criterion 2 requires the opposite for "Remember me":

> Given correct credentials and "Remember me" checked, when sign-in succeeds, then a refresh token is issued so the session survives a browser restart.

A token issued but discarded on hard refresh does not satisfy the criterion. Without a persistence strategy in the plan, FI1 implementers would either (a) silently break L2-002, or (b) invent their own storage strategy without security review.

**Fix:** rewrite §7 to specify:

- Access token stays in memory (signal in `AuthStateService`). Never persisted.
- Refresh token persistence is conditional on the **Remember me** checkbox in `SignInFormComponent`:
  - **Unchecked** (default): refresh token also kept in memory only. Hard refresh ends the session — matches L2-002 default behavior.
  - **Checked**: refresh token persisted to `localStorage` under key `forge.auth.refreshToken`. On app boot, `AuthStateService` reads the stored refresh token, exchanges it for a fresh access-token pair via `IAuthService.refresh(...)`, and clears the persisted token if the exchange fails. The new pair replaces the persisted entry (refresh-token rotation per L2-033).
- XSS exposure of `localStorage` is mitigated by `L2-052` (CSP blocks inline scripts and `unsafe-eval`); the plan should reference the CSP requirement explicitly here.
- An alternative path (httpOnly secure cookie issued by the backend) is **explicitly out of scope** for the MVP because BP1's auth flow returns refresh tokens in the JSON response body. Calling this out so a future hardening pass can swap the storage without invalidating the rest of the plan.

Add a corresponding row to §12 — L2-002 currently maps to "`SignInFormComponent` (§4) — existing"; it should also reference §7 for the persistence behavior.

### Non-blocking observations

- The `MeService` (`GET /api/me`, `DELETE /api/me`) and `ProfileService` (`PUT /api/profile`, `POST /api/profile/weight`, etc.) are split. That's a defensible read/own vs. mutate-everything-else split, but reviewers may flag it as duplicative because they share the underlying `Users` aggregate. Either acceptable; flagging so the FT1 / FI1 implementer doesn't relitigate.
- The `responsive.spec.ts` and `accessibility.spec.ts` are general-purpose specs that walk every authenticated route. They'll grow as features land. The plan should expect minor edits to these specs in every FI1.x slice rather than treating them as static — non-blocking, just an expectation note for FI1.
- `forge` (the application) does not import from `components` directly in the plan (every shared presentation component is reached via `domain` composition). That's tighter than the rubric requires (`app may import from all three`), but it's a reasonable convention. Calling out so reviewers don't flag it as a gap.

## Pass 2 - findings

Re-walked the FP2 explicit checks against `./docs/plans/frontend.md` after applying the two Pass 1 fixes.

- **Finding 1 (Angular Material wrappers)** — resolved. §3 now has a "Wraps" column for every entry. The 12 wrapping components each name their Material 3 host (`<mat-card>`, `<mat-flat-button>`, `<mat-icon-button>`, `<mat-form-field>` / `<input matInput>`, `<mat-checkbox>`, `<mat-slide-toggle>`, `<mat-chip>`, `<mat-progress-spinner>`, `<mat-toolbar>`, `<mat-tab-nav-bar>`, `<mat-nav-list>`). The two pure layout primitives (`EmptyStateComponent`, `ErrorBannerComponent`) are explicitly called out as "no Material counterpart" so reviewers don't flag them. The §3 preamble cites Implementation Guidance — Frontend and notes that `mat.theme(...)` was already wired in MF1.
- **Finding 2 (Refresh-token persistence vs L2-002 ac 2)** — resolved. §7 storage policy rewritten to:
  - Access token always in memory.
  - Refresh token in memory by default; **`localStorage` only when "Remember me" is checked**, with refresh-token rotation on each successful exchange and clear-on-failure.
  - L2-052 CSP cited explicitly as the XSS mitigation; httpOnly-cookie alternative noted as future work touching only `AuthStateService` + `IAuthService.refresh`.
  - §12 row for L2-002 updated to also reference §7.

Re-checks across all FP2 explicit checks: library structure, contract-file inventory, Angular Material specification, design tokens, BEM, per-file split, Playwright POM coverage, local username/password auth, mock-to-screen mapping, and the CRITICAL library-placement audit all still pass after the additions. Both new edits stayed within their respective sections; no other section was touched.

Pass 2 produces zero blocking findings. FP2 is complete.
