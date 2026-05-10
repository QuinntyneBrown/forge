# Frontend implementation tasks

Vertical-slice task list for `FI1`. Each task implements a single end-to-end UI slice on top of the MF1 MVP and against `./docs/plans/frontend.md`. Per-task contents:

- **Requirements + mocks** — L2 IDs the slice satisfies and mock screens it renders.
- **Slice** — exactly the artifacts that must change or be added (route / page / domain components / api service+contract / model DTOs / styles).
- **Acceptance test** — the Playwright POM spec that gates the slice. Test header carries `// Traces to: <L2-IDs>`.
- **Guidance** — Implementation Guidance bullets that apply.

Slices are sequenced by dependency. Where two slices have no dependency, they may be picked in either order in FI1.

Conventions:
- Every component is a `.ts` / `.html` / `.scss` triple under its own folder. No inline `template:` or `styles:`.
- Every backend-facing service has a sibling `*.service.contract.ts` exporting an interface + `InjectionToken`. Domain components inject via `@Inject(*_SERVICE)` — never the concrete class.
- BEM class names everywhere. Every reusable presentation component lives in `components` and wraps an Angular Material 3 component (per FP1 §3) unless explicitly marked as a pure layout primitive.
- Library imports respect: `components → nothing`, `domain → api + components`, `forge → api + domain`. Each task is implemented inside its target library only — never crosses a forbidden boundary.
- **"Verified inside" tasks ship with their downstream consumer.** When a task's "Acceptance test" line says *"verified inside `<other-spec>`"*, the wrapper / service is **not** implemented as a standalone PR — it lands in the same commit as the FT-* listed in its acceptance line, and that consumer's spec is the gating acceptance test. The pair (wrapper + first consumer) is the vertical slice. This applies to FT-003, FT-005, FT-006, FT-007, FT-008, FT-009, FT-010, FT-011, and FT-016. Each affected task ends with a "Ships with" line naming the consumer task it rides along with.

## Phase FI1.0 — Material wrapping in `components` (foundation)

These eleven slices replace the MF1 hand-rolled CSS components with Angular Material 3 wrappers. They unblock every later phase. Each task ships a single component triple plus an updated `public-api.ts` export. Acceptance is verified by an existing or new spec that already uses the component.

### FT-001 — Wrap `CardComponent` around `<mat-card>` ✅ done

- **Requirements:** L2-046, L2-052; foundation for every authenticated screen.
- **Slice:**
  - `projects/components/src/lib/card/card.component.ts` — keep the `forge-card` selector and `[title]` input; swap the template to wrap `<mat-card>` + `<mat-card-title>` + `<mat-card-content>` projecting `<ng-content>`.
  - SCSS retains the existing `card`, `card__header`, `card__title`, `card__body` BEM classes and removes any styling that Material now provides.
  - Update `projects/components/src/public-api.ts` (no API change for consumers).
- **Acceptance test:** existing `tests/sign-in.spec.ts` continues to pass; the dashboard `<forge-card title="Server status">` continues to render the title + health badge.
- **Guidance:** Frontend (Angular Material 3), General (one-type-per-file).

### FT-002 — `ButtonComponent` wrapping `<button mat-flat-button>` / `mat-stroked-button` / `mat-button` ✅ done

- **Requirements:** L2-046; every CTA across the app.
- **Slice:**
  - `projects/components/src/lib/button/button.component.{ts,html,scss}` exposes `[variant: 'filled' | 'outlined' | 'text']`, `[disabled]`, `[loading]`. Template selects the matching Material directive via `@if` blocks.
  - Public API export.
- **Acceptance test:** existing `sign-in.spec.ts` (sign-in submit button) passes; new `tests/components/button.smoke.spec.ts` mounts each variant in a story-style harness route and asserts the rendered DOM contains the expected `mat-*` host class.
- **Guidance:** Frontend, General.

### FT-003 — `IconButtonComponent` wrapping `<button mat-icon-button>` ✅ done

- **Requirements:** L2-046.
- **Slice:**
  - `projects/components/src/lib/icon-button/icon-button.component.{ts,html,scss}`. `<ng-content>` projects the Material Symbols glyph. 48×48dp hit area.
  - Public API export.
- **Acceptance test:** verified indirectly by FT-027 workout list (three-dot menu) and FT-031 profile page (password reveal). No standalone spec required.
- **Ships with:** FT-027 (first consumer).
- **Guidance:** Frontend.

### FT-004 — `FieldComponent` wrapping `<mat-form-field appearance="outline">` ⚠ design pending

- **Requirements:** L2-005, L2-007.
- **Original slice (rejected during FT-012 implementation):** project the inner `<input matInput>` via `<ng-content>` into a `<mat-form-field>` defined in `FieldComponent`'s template. Verified to break at runtime — `MatFormField`'s `@ContentChild(MatFormFieldControl)` does not resolve the projected `MatInput` instance through the wrapper's view, producing `Error: mat-form-field must contain a MatFormFieldControl.`. FT-012 ships `<mat-form-field>` + `<input matInput>` directly inline as a result.
- **Revised approach (deferred to a follow-up FI1 slice):** swap `<ng-content>` projection for a directive- or template-input-based pattern. Two viable designs:
  1. Take the input value as an `@Input()` and have `FieldComponent` render its own `<input matInput [type]="type">` internally — loses textarea/select flexibility but eliminates the projection issue.
  2. Drop the `<mat-form-field>` wrapper and have `FieldComponent` be a directive (`forgeField`) on the consumer's `<mat-form-field>` that supplies label / hint / error inputs declaratively — keeps the consumer's template simple while still abstracting the field anatomy.
- **Acceptance test:** to be written when the revised design lands; will verify a sign-up / profile slice still passes after migrating `<mat-form-field>` to `<forge-field>` (or `[forgeField]`).
- **Ships with:** the first consumer that successfully exercises the revised pattern (likely a profile-form or workout-detail-form slice).
- **Guidance:** Frontend.

### FT-005 — `CheckboxComponent` wrapping `<mat-checkbox>` ✅ done

- **Requirements:** L2-002 (Remember me).
- **Slice:**
  - `projects/components/src/lib/checkbox/checkbox.component.{ts,html,scss}` exposes `[checked]` / `(checkedChange)`.
- **Acceptance test:** the Remember me toggle in FT-015 (auth persistence) is asserted by `sign-in-remember-me.spec.ts` (added in FT-015).
- **Ships with:** FT-015.
- **Guidance:** Frontend.

### FT-006 — `SwitchComponent` wrapping `<mat-slide-toggle>` ✅ done

- **Requirements:** L2-026, L2-027.
- **Slice:**
  - `projects/components/src/lib/switch/switch.component.{ts,html,scss}`.
- **Acceptance test:** verified inside `profile.spec.ts` (FT-031), which toggles the kitchen-closes nudge and the leaderboard opt-in.
- **Ships with:** FT-031.
- **Guidance:** Frontend.

### FT-007 — `ChipComponent` wrapping `<mat-chip>` inside `<mat-chip-listbox>` ✅ done

- **Requirements:** L2-008.
- **Slice:**
  - `projects/components/src/lib/chip/chip.component.{ts,html,scss}`. `[selected]` modifier.
- **Acceptance test:** verified inside `workouts-list.spec.ts` (FT-027) when the test selects the Treadmill chip.
- **Ships with:** FT-027.
- **Guidance:** Frontend.

### FT-008 — `ProgressRingComponent` wrapping `<mat-progress-spinner mode="determinate">`

- **Requirements:** L2-011, L2-021.
- **Slice:**
  - `projects/components/src/lib/progress-ring/progress-ring.component.{ts,html,scss}` exposes `[value]` and `[max]`. Computes `value/max * 100` as the spinner's `value` input.
- **Acceptance test:** verified inside `dashboard.spec.ts` (FT-024) and `rewards-redeem.spec.ts` (FT-030).
- **Ships with:** FT-019 (DailyRingCardComponent — first consumer).
- **Guidance:** Frontend.

### FT-009 — `BadgeComponent` wrapping `<mat-chip>` styled as a status pill ✅ done

- **Requirements:** L2-022, L2-024.
- **Slice:**
  - `projects/components/src/lib/badge/badge.component.{ts,html,scss}` exposes `[variant: 'success' | 'warning' | 'error' | 'neutral']`. SCSS maps each variant to a token-driven background + foreground.
- **Acceptance test:** existing `sign-in.spec.ts` already asserts `<forge-health-badge>` resolves to "Healthy"; that domain component will compose `BadgeComponent` post-FT-009 and the assertion stands.
- **Ships with:** the `<forge-health-badge>` composition swap that satisfies the existing sign-in.spec.ts. The wrapper + the swap land in one commit.
- **Guidance:** Frontend.

### FT-010 — `EmptyStateComponent` (pure layout primitive — hand-rolled)

- **Requirements:** L2-028.
- **Slice:**
  - `projects/components/src/lib/empty-state/empty-state.component.{ts,html,scss}` projects an illustration slot, headline (`[title]`), copy (`[message]`), primary CTA (`<ng-content>`).
- **Acceptance test:** `empty-state.spec.ts` (FT-034).
- **Ships with:** FT-027 (zero-row variant) or FT-034, whichever lands first.
- **Guidance:** Frontend (intentional non-Material primitive — Material has no equivalent; explicitly noted in FP1 §3).

### FT-011 — `ErrorBannerComponent` (pure layout primitive — hand-rolled)

- **Requirements:** L2-024, L2-029.
- **Slice:**
  - `projects/components/src/lib/error-banner/error-banner.component.{ts,html,scss}` exposes `[title]`, `[message]`, retry CTA via `<ng-content>`.
- **Acceptance test:** `error-state.spec.ts` (FT-033).
- **Ships with:** FT-033.
- **Guidance:** Frontend (intentional non-Material primitive).

## Phase FI1.1 — Auth surfaces (parallel with BI1.1)

### FT-012 — Sign-up form ✅ done

- **Requirements:** L2-001. Mock: `sign-up.html`.
- **Slice:**
  - `IAuthService.register(...)` extension on the existing `auth.service.contract.ts` + concrete `register` method on `AuthService`.
  - `RegisterRequest` DTO under `projects/api/src/lib/models/register-request.model.ts`.
  - `projects/domain/src/lib/sign-up-form/sign-up-form.component.{ts,html,scss}` — composes `CardComponent`, `FieldComponent`, `ButtonComponent`. Reactive form with email + first/last name + password matching the policy in L2-001.
  - `projects/forge/src/app/pages/sign-up/sign-up.page.{ts,html,scss}` and a route `'/sign-up'`.
- **Acceptance test:** `tests/sign-up.spec.ts` — register a fresh user → auto sign-in → land on `/dashboard`.
- **Guidance:** Frontend, Authentication, General.

### FT-013 — Password reset request + confirm ✅ done

- **Requirements:** L2-004. Mock: `password-reset.html`.
- **Slice:**
  - `IAuthService.requestPasswordReset(email)` and `IAuthService.confirmPasswordReset(token, newPassword)` on the contract; concrete impls on `AuthService`.
  - DTO models `password-reset-request.model.ts`, `password-reset-confirm.model.ts`.
  - `projects/domain/src/lib/password-reset-request-form/...` and `.../password-reset-confirm-form/...` — both compose `CardComponent`, `FieldComponent`, `ButtonComponent`.
  - `projects/forge/src/app/pages/password-reset/password-reset.page.{ts,html,scss}` reads `?token` query param to switch between the two forms; route `'/password-reset'`.
- **Acceptance test:** `tests/password-reset.spec.ts` — request reset (always 202), call backend to fetch the issued token from the no-op email log, confirm with new password, sign in with new password, asserts old password fails.
- **Guidance:** Frontend, Authentication.

### FT-014 — `refreshInterceptor` + `authGuard` ✅ done

- **Requirements:** L2-002 (refresh), L2-033, L2-038 (frontend side).
- **Slice:**
  - `projects/forge/src/app/refresh.interceptor.ts` — on `401` from any API request, calls `IAuthService.refresh(...)` once, retries the original request with the rotated access token, updates `AuthStateService`. On refresh failure: clears state, routes to `/sign-in`.
  - `projects/forge/src/app/auth.guard.ts` — `canActivate` redirects to `/sign-in?returnUrl=...` when `AuthStateService.snapshot()` is null.
  - Wire both in `app.config.ts`. Add `canActivate: [authGuard]` to every authenticated route.
- **Acceptance test:** `tests/refresh-and-guard.spec.ts` — sign in, force-expire the access token (drop it from `AuthStateService` via a test-only debug API or by calling a backend endpoint that rejects the current token), make an authed call → it transparently refreshes and succeeds. Visit `/dashboard` unauthed → redirected to `/sign-in?returnUrl=/dashboard`.
- **Guidance:** Frontend, Authentication.

### FT-015 — Remember-me persistence (refresh token in `localStorage`) ✅ done

- **Requirements:** L2-002 (acceptance criterion 2), L2-033.
- **Slice:**
  - Extend `AuthStateService` with `setSession(result, persist: boolean)`. When `persist=true`, write the refresh token to `localStorage['forge.auth.refreshToken']`. On app boot, read the persisted token and call `IAuthService.refresh(...)`; on success update the persisted entry (rotation), on failure clear it and route to `/sign-in`.
  - `SignInFormComponent` adds a `<forge-checkbox>` for "Remember me"; emits the boolean alongside `signedIn`.
- **Acceptance test:** `tests/sign-in-remember-me.spec.ts` — sign in with Remember me checked, close + reopen the browser context (Playwright `storageState`), verify session resumes on `/dashboard` without re-prompt. Sign in with Remember me unchecked → fresh context shows `/sign-in`.
- **Guidance:** Authentication, Frontend.

### FT-016 — `MeService` (read leg + delete account) ✅ done

- **Requirements:** L2-005 (read), L2-006.
- **Slice:**
  - `projects/api/src/lib/me.service.contract.ts` (`IMeService`, `ME_SERVICE`).
  - `projects/api/src/lib/me.service.ts` exposes `getMe()` (calls `GET /api/me`) and `deleteMe()` (calls `DELETE /api/me`).
  - `current-user.model.ts` DTO.
  - DI registration in `app.config.ts`.
- **Acceptance test:** verified inside `profile.spec.ts` (FT-031) and `account-deletion.spec.ts` (FT-032). No standalone spec.
- **Ships with:** FT-031.
- **Guidance:** Frontend (interface-driven service consumption).

## Phase FI1.2 — App shell (parallel with BI1.2)

### FT-017 — `AppShellComponent` + `BottomNavComponent` + `NavRailComponent` ✅ done

- **Requirements:** L2-010, L2-013, L2-046, L2-030. Mocks: every authenticated screen.
- **Slice:**
  - `projects/components/src/lib/app-shell/app-shell.component.{ts,html,scss}` wraps `<mat-toolbar>` for the top app bar; switches between `BottomNavComponent` (<992px) and `NavRailComponent` (≥992px).
  - `projects/components/src/lib/bottom-nav/bottom-nav.component.{ts,html,scss}` wraps `<mat-tab-nav-bar>` with the four primary destinations (Home / Workouts / Rewards / Profile).
  - `projects/components/src/lib/nav-rail/nav-rail.component.{ts,html,scss}` wraps `<mat-nav-list>` with the same destinations.
  - Existing `DashboardPage` and the upcoming `WorkoutsPage` / `RewardsPage` / `ProfilePage` wrap their content in `<forge-app-shell>`.
- **Acceptance test:** `tests/responsive.spec.ts` — at 360 the bottom nav is visible, at 1440 the nav rail is visible. Sign-in is unaffected (the shell is only on authed routes).
- **Guidance:** Frontend, General.

## Phase FI1.3 — Dashboard (parallel with BI1.3)

### FT-018 — `DashboardService` + contract

- **Requirements:** L2-011, L2-012, L2-013, L2-014, L2-022.
- **Slice:**
  - `projects/api/src/lib/dashboard.service.contract.ts` (`IDashboardService`, `DASHBOARD_SERVICE`) and `dashboard.service.ts` calling `GET /api/dashboard`.
  - `dashboard-summary.model.ts` DTO (today's calories/target, today's minutes, current streak, points balance, current tier, next reward, month-to-date weight delta).
  - DI registration.
- **Acceptance test:** verified inside `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-019 — `DailyRingCardComponent`

- **Requirements:** L2-011, L2-012. Mock: `dashboard.html`.
- **Slice:**
  - `projects/domain/src/lib/daily-ring-card/daily-ring-card.component.{ts,html,scss}` injects `IDashboardService` via `DASHBOARD_SERVICE`. Composes `CardComponent` + `ProgressRingComponent`. Renders `980 / 1500 kcal` numeric label + minutes-today tile.
- **Acceptance test:** asserted in `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-020 — `StreakCardComponent`

- **Requirements:** L2-013, L2-020. Mock: `dashboard.html`.
- **Slice:**
  - `projects/domain/src/lib/streak-card/streak-card.component.{ts,html,scss}` injects `IDashboardService`. Composes `CardComponent` + `BadgeComponent`. Renders current streak in days + multiplier.
- **Acceptance test:** asserted in `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-021 — `WeightProgressCardComponent`

- **Requirements:** L2-014, L2-015. Mock: `dashboard.html`.
- **Slice:**
  - `projects/domain/src/lib/weight-progress-card/weight-progress-card.component.{ts,html,scss}` injects `IDashboardService`. Renders `−20 lb / month · On track · −5.2 lb so far in May`.
- **Acceptance test:** asserted in `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-022 — `TierCardComponent` + `RewardsService.getCurrentTier`

- **Requirements:** L2-022. Mocks: `dashboard.html`, `profile.html`, `rewards.html`.
- **Slice:**
  - `projects/api/src/lib/rewards.service.contract.ts` (`IRewardsService`, `REWARDS_SERVICE`) — initial method `getCurrentTier()`.
  - `projects/api/src/lib/rewards.service.ts` calling `GET /api/tier`.
  - `tier.model.ts` DTO.
  - `projects/domain/src/lib/tier-card/tier-card.component.{ts,html,scss}` injects both `DASHBOARD_SERVICE` (for points balance) and `REWARDS_SERVICE` (for tier).
- **Acceptance test:** asserted in `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-023 — `LeaderboardCardComponent` + `LeaderboardService`

- **Requirements:** L2-027. Mock: `dashboard.html`.
- **Slice:**
  - `projects/api/src/lib/leaderboard.service.contract.ts` + concrete service calling `GET /api/leaderboard`.
  - `leaderboard-entry.model.ts` DTO.
  - `projects/domain/src/lib/leaderboard-card/leaderboard-card.component.{ts,html,scss}`.
- **Acceptance test:** asserted in `dashboard.spec.ts` (FT-024).
- **Guidance:** Frontend.

### FT-024 — `DashboardPage` rewrite + spec

- **Requirements:** L2-011..L2-014, L2-022, L2-027. Mock: `dashboard.html`.
- **Slice:**
  - Rewrite `projects/forge/src/app/pages/dashboard/dashboard.page.{ts,html,scss}` to compose `<forge-app-shell>`, `<forge-daily-ring-card>`, `<forge-streak-card>`, `<forge-weight-progress-card>`, `<forge-tier-card>`, `<forge-leaderboard-card>`. Existing health-badge composition moved off the dashboard (lives on `error-state.html` only — see FT-033).
- **Acceptance test:** `tests/dashboard.spec.ts` — sign in to a seeded test user, dashboard shows 980 / 1500 kcal ring, 70 min tile, current streak, tier `Forged Iron` (or whatever the seed dictates), `−20 lb / month` goal tile, leaderboard tile.
- **Guidance:** Frontend, General.

## Phase FI1.4 — Workouts

### FT-025 — `SessionsService` + contract

- **Requirements:** L2-007, L2-008, L2-009.
- **Slice:**
  - `projects/api/src/lib/sessions.service.contract.ts` (`ISessionsService`, `SESSIONS_SERVICE`) with `list(filters)`, `getById(id)`, `create(req)`, `update(id, req)`, `duplicate(id)`, `delete(id)`.
  - DTOs: `session.model.ts`, `session-list-query.model.ts`, `create-session-request.model.ts`, `update-session-request.model.ts`.
- **Acceptance test:** verified inside FT-027/FT-028/FT-029.
- **Guidance:** Frontend.

### FT-026 — `EquipmentService` + contract

- **Requirements:** L2-010.
- **Slice:**
  - `projects/api/src/lib/equipment.service.contract.ts` + concrete service calling `GET /api/equipment`.
  - `equipment-type.model.ts` (string-literal union).
- **Acceptance test:** verified inside FT-027 / FT-028.
- **Guidance:** Frontend.

### FT-027 — `WorkoutListComponent` + `WorkoutsPage` + filter chips

- **Requirements:** L2-007, L2-008, L2-010, L2-028. Mocks: `workouts.html`, `empty-state.html`.
- **Slice:**
  - `projects/domain/src/lib/workout-list/workout-list.component.{ts,html,scss}` injects `SESSIONS_SERVICE` + `EQUIPMENT_SERVICE`. Composes `CardComponent`, `ChipComponent`, `EmptyStateComponent`, `ButtonComponent`. Renders chip filter strip (Treadmill / Indoor Bike / Bench Press / Elliptical / This week / This month / All) and the session list. Empty state when zero rows.
  - `projects/forge/src/app/pages/workouts/workouts.page.{ts,html,scss}` wraps in `<forge-app-shell>`. Route `/workouts`.
- **Acceptance test:** `tests/workouts-list.spec.ts` — seed 10 sessions, navigate to `/workouts`, click Treadmill chip, only treadmill rows remain. Click into a session → routes to `/workouts/:id`.
- **Guidance:** Frontend, General.

### FT-028 — `WorkoutDetailFormComponent` + `WorkoutNewPage` + create flow

- **Requirements:** L2-007, L2-009. Mock: `workout-detail.html` (create variant).
- **Slice:**
  - `projects/domain/src/lib/workout-detail-form/workout-detail-form.component.{ts,html,scss}` — equipment select (driven by `EQUIPMENT_SERVICE`), date, start time, duration, distance (hidden when equipment is `BenchPress`), avg HR, active calories, notes textarea. Reactive form with the L2-007 boundaries (duration 1–480, etc.).
  - `projects/forge/src/app/pages/workout-new/workout-new.page.{ts,html,scss}` route `/workouts/new`.
- **Acceptance test:** `tests/workout-create.spec.ts` — create a 22-min treadmill session with distance 2.1, avg HR 128, active calories 218 → server responds 201; navigate to `/dashboard`, ring shows the new session's contribution within 1 second.
- **Guidance:** Frontend, Validation (client-side mirrors server-side ranges).

### FT-029 — `WorkoutDetailPage` + `WorkoutPointsBreakdownComponent` + delete refund

- **Requirements:** L2-009, L2-018, L2-019, L2-020. Mock: `workout-detail.html`.
- **Slice:**
  - `projects/domain/src/lib/workout-points-breakdown/workout-points-breakdown.component.{ts,html,scss}` consumes `SESSIONS_SERVICE.getById` → renders the points ledger rows for the session (Base, Morning bonus, Streak multiplier).
  - `projects/forge/src/app/pages/workout-detail/workout-detail.page.{ts,html,scss}` route `/workouts/:id` composes `WorkoutDetailFormComponent` (edit mode) + `WorkoutPointsBreakdownComponent`.
  - "Delete session" / "Duplicate" / "Save changes" CTAs wire to `update`, `duplicate`, `delete`.
- **Acceptance test:** `tests/workout-delete-refund.spec.ts` — open a +85 pts session, delete, dashboard balance returns to pre-session value within 1s.
- **Guidance:** Frontend.

## Phase FI1.5 — Rewards + profile

### FT-030 — `RewardsCatalogComponent` + `RewardsPage` + redeem flow

- **Requirements:** L2-021, L2-022. Mock: `rewards.html`.
- **Slice:**
  - Extend `IRewardsService` with `listRewards()` and `redeem(rewardId)`. DTO `reward.model.ts`, `redemption-result.model.ts`.
  - `projects/domain/src/lib/rewards-catalog/rewards-catalog.component.{ts,html,scss}` — composes `CardComponent`, `ButtonComponent`, `ProgressRingComponent` (per-reward progress to next-affordable).
  - `projects/forge/src/app/pages/rewards/rewards.page.{ts,html,scss}` wraps in `<forge-app-shell>`. Route `/rewards`. Composes `<forge-tier-card>` + `<forge-rewards-catalog>`.
- **Acceptance test:** `tests/rewards-redeem.spec.ts` — seed user with 1,250 pts, redeem 500 reward → balance 750 + redemption row. Try to redeem 5,000 reward → 400 banner appears, balance unchanged.
- **Guidance:** Frontend.

### FT-031 — `ProfileService` + `ProfileFormComponent` + `ProfilePage` ⚠ BT-015 fields ✅ done; window/toggle fields wait for BT-018/019/020

- **Requirements:** L2-005, L2-014..L2-017, L2-026, L2-027. Mock: `profile.html`.
- **Slice:**
  - `projects/api/src/lib/profile.service.contract.ts` (`IProfileService`, `PROFILE_SERVICE`) + concrete service for the six profile endpoints.
  - DTOs: `update-profile-request.model.ts`, `weight-entry.model.ts`, `weight-goal-request.model.ts`, `morning-window-request.model.ts`, `kitchen-window-request.model.ts`.
  - `projects/domain/src/lib/profile-form/profile-form.component.{ts,html,scss}` — composes `CardComponent`, `FieldComponent`, `SwitchComponent`, `ButtonComponent`. Sections: account, goals, windows, integrations, save.
  - `projects/forge/src/app/pages/profile/profile.page.{ts,html,scss}` route `/profile` composes `<forge-app-shell>` + `<forge-profile-form>` + `<forge-tier-card>`.
- **Acceptance test:** `tests/profile.spec.ts` — edit first name, time zone (assert America/Toronto displays in full at all viewports), daily targets, monthly goal, morning + kitchen windows, leaderboard opt-in. Reload → values persist.
- **Guidance:** Frontend, Validation.

### FT-032 — Account deletion flow

- **Requirements:** L2-006, L2-050. Mock: `profile.html` (delete CTA).
- **Slice:**
  - `ProfileFormComponent` adds a confirm-twice "Delete account" CTA that calls `IMeService.deleteMe()`. On success: `AuthStateService.clear()`, persisted refresh token cleared, route to `/sign-in`.
- **Acceptance test:** `tests/account-deletion.spec.ts` — delete account, attempt sign-in with the same email → 401 banner.
- **Guidance:** Frontend, Authentication.

## Phase FI1.6 — Empty + error states

### FT-033 — `SyncErrorPanelComponent` + `HealthKitService` + `ErrorPage`

- **Requirements:** L2-024, L2-029. Mock: `error-state.html`.
- **Slice:**
  - `projects/api/src/lib/healthkit.service.contract.ts` + concrete service calling `POST /api/healthkit/ingest`.
  - `projects/domain/src/lib/sync-error-panel/sync-error-panel.component.{ts,html,scss}` — composes `CardComponent`, `ErrorBannerComponent`, `BadgeComponent`, `ButtonComponent`. Reads diagnostics from `HEALTH_SERVICE` and `HEALTHKIT_SERVICE`.
  - `projects/forge/src/app/pages/error/error.page.{ts,html,scss}` route `/error` composes the panel. Surfaces the `traceId` query param.
- **Acceptance test:** `tests/error-state.spec.ts` — force a HealthKit sync failure (test endpoint), navigate to `/error?traceId=...`, assert the panel renders the trace id and the diagnostics. Click "Go to dashboard" → `/dashboard`.
- **Guidance:** Frontend.

### FT-034 — `NotFoundPage` + empty-state spec

- **Requirements:** L2-028, L2-029.
- **Slice:**
  - `projects/forge/src/app/pages/not-found/not-found.page.{ts,html,scss}` composes `<forge-empty-state>` configured as a 404 message. Route `**`.
  - Workouts and rewards empty-state coverage already lands inside FT-027 / FT-030; the dedicated spec exercises 404.
- **Acceptance test:** `tests/empty-state.spec.ts` — for a brand-new account, `/workouts` shows "No workouts yet" empty state with a "Log first workout" button. Navigate to `/does-not-exist` → renders `NotFoundPage`.
- **Guidance:** Frontend.

## Phase FI1.7 — Accessibility

### FT-035 — `accessibility.spec.ts` (axe-core scan)

- **Requirements:** L2-045, L2-047, L2-048.
- **Slice:**
  - Add dev dep `@axe-core/playwright`.
  - `tests/accessibility.spec.ts` — for each authenticated route (`/dashboard`, `/workouts`, `/workouts/new`, `/workouts/:id`, `/rewards`, `/profile`) and the unauthenticated routes (`/sign-in`, `/sign-up`, `/password-reset`, `/error`), run `AxeBuilder().withTags(['wcag2a', 'wcag2aa']).analyze()` and assert zero violations.
  - Fix any violations surfaced (typically: missing `aria-label` on icon buttons, missing form-input labels, contrast issues).
- **Acceptance test:** the spec itself.
- **Guidance:** Frontend (a11y).

## Sequencing summary

| Phase  | Tasks                          | Comment |
|--------|--------------------------------|---------|
| FI1.0  | FT-001..FT-011                 | Material wrapping in `components`. Foundation; later phases depend on these wrappers. FT-001 first; the other ten can land in any order after Material is wired through `mat.theme(...)`. |
| FI1.1  | FT-012..FT-016                 | Auth surfaces. Parallel with backend BI1.1 (BT-001..BT-013). FT-014 + FT-015 close the L2-002 + L2-033 + Remember-me gaps. |
| FI1.2  | FT-017                         | App shell. Single task because the three components ship together as one cohesive composition. |
| FI1.3  | FT-018..FT-024                 | Dashboard. Parallel with BI1.3 backend (BT-022..BT-029). FT-018 first; FT-019..FT-023 cards in any order; FT-024 ties the page together. |
| FI1.4  | FT-025..FT-029                 | Workouts. FT-025 + FT-026 first (services). |
| FI1.5  | FT-030..FT-032                 | Rewards + profile. |
| FI1.6  | FT-033..FT-034                 | Empty + error states. |
| FI1.7  | FT-035                         | Accessibility pass — runs last so every authed route exists when axe runs. |

Each task is small enough that one FI1 loop iteration can: write the Playwright POM acceptance test, implement the slice, run the rubric eval, fix on find, and mark the task done.
