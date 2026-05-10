# Frontend implementation plan

This plan turns the approved L1/L2 requirements and the eleven accepted mocks into a concrete Angular implementation that extends the MF1 MVP. Every item below cites the L2(s) it satisfies, the mock(s) it renders, and the Implementation Guidance section that constrains it. The MVP shape (`api` / `components` / `domain` / `forge` libraries with the documented dependency direction, interface-driven service consumption, BEM, one-type-per-file `.ts`/`.html`/`.scss` triples, Material 3 design tokens, Playwright POM acceptance tests) is the reference — nothing in this plan deviates from it.

## 1. Workspace layout

Inherits from MF1. No new libraries.

```
frontend/
  package.json
  angular.json
  tsconfig.json
  playwright.config.ts
  projects/
    api/         (no deps)
    components/  (no deps on api or domain)
    domain/      (deps: api, components)
    forge/       (deps: api, components, domain)
  tests/
    pom/
    *.spec.ts
```

Constraints (Implementation Guidance — Frontend, Library Structure, General):
- Component triples — every `*.component.ts` has a sibling `.html` + `.scss`. No inline `template:` or `styles:`. (L2-054)
- BEM class names everywhere (block / block__element / block--modifier).
- Library imports verified by grep:
  - `components` → nothing from `api` / `domain` / `forge`.
  - `domain` → `api` + `components` only; never `forge`.
  - `forge` → `api` + `domain` (and transitively `components` via `domain`).

## 2. Service inventory — `api` library

Every backend-facing service ships as a `*.service.ts` (concrete) + `*.service.contract.ts` (interface + injection token). DTO types live under `lib/models/`. The composition root in `forge` binds tokens to concrete impls (Implementation Guidance — Frontend, Library Structure).

| Concrete service              | Contract file                          | Interface           | Injection token   | Endpoints                                                                                              | L2s | Mock(s) |
|-------------------------------|----------------------------------------|---------------------|-------------------|--------------------------------------------------------------------------------------------------------|-----|---------|
| `AuthService` (existing)      | `auth.service.contract.ts` (existing)  | `IAuthService`      | `AUTH_SERVICE`    | `POST /api/auth/sign-in`                                                                                | L2-002 | `sign-in.html` |
|                               |                                        |                     |                   | + add `register`, `refresh`, `signOut`, `requestPasswordReset`, `confirmPasswordReset`                   | L2-001..L2-004, L2-033, L2-036 | `sign-up.html`, `password-reset.html` |
| `HealthService` (existing)    | `health.service.contract.ts` (existing)| `IHealthService`    | `HEALTH_SERVICE`  | `GET /health`                                                                                          | L2-044 | `error-state.html` (status diag) |
| `MeService`                   | `me.service.contract.ts`               | `IMeService`        | `ME_SERVICE`      | `GET /api/me`, `DELETE /api/me`                                                                         | L2-005, L2-006 | `profile.html` |
| `ProfileService`              | `profile.service.contract.ts`          | `IProfileService`   | `PROFILE_SERVICE` | `PUT /api/profile`, `POST /api/profile/weight`, `PUT /api/profile/weight-goal`, `PUT /api/profile/morning-window`, `PUT /api/profile/kitchen-window`, `PUT /api/profile/leaderboard-opt-in` | L2-005, L2-014..L2-017, L2-026, L2-027 | `profile.html` |
| `SessionsService`             | `sessions.service.contract.ts`         | `ISessionsService`  | `SESSIONS_SERVICE`| `GET /api/sessions` (filters), `GET /api/sessions/{id}`, `POST /api/sessions`, `PUT /api/sessions/{id}`, `POST /api/sessions/{id}/duplicate`, `DELETE /api/sessions/{id}` | L2-007..L2-009 | `workouts.html`, `workout-detail.html`, `empty-state.html` |
| `EquipmentService`            | `equipment.service.contract.ts`        | `IEquipmentService` | `EQUIPMENT_SERVICE`| `GET /api/equipment`                                                                                  | L2-010 | `workout-detail.html` (equipment dropdown) |
| `DashboardService`            | `dashboard.service.contract.ts`        | `IDashboardService` | `DASHBOARD_SERVICE`| `GET /api/dashboard`                                                                                  | L2-011..L2-013, L2-022 | `dashboard.html` |
| `RewardsService`              | `rewards.service.contract.ts`          | `IRewardsService`   | `REWARDS_SERVICE` | `GET /api/rewards`, `POST /api/rewards/{id}/redeem`, `GET /api/tier`                                    | L2-021, L2-022 | `rewards.html` |
| `LeaderboardService`          | `leaderboard.service.contract.ts`      | `ILeaderboardService`| `LEADERBOARD_SERVICE`| `GET /api/leaderboard`                                                                              | L2-027 | `dashboard.html` (peer rank tile) |
| `HealthKitService`            | `healthkit.service.contract.ts`        | `IHealthKitService` | `HEALTHKIT_SERVICE`| `POST /api/healthkit/ingest`                                                                          | L2-023 | `error-state.html` (sync diag) |

DTO models under `projects/api/src/lib/models/`:

- (existing) `auth-result.model.ts`, `sign-in-request.model.ts`, `health-status.model.ts`
- `register-request.model.ts`, `password-reset-request.model.ts`, `password-reset-confirm.model.ts`, `refresh-request.model.ts`
- `current-user.model.ts`, `update-profile-request.model.ts`, `weight-entry.model.ts`, `weight-goal-request.model.ts`, `morning-window-request.model.ts`, `kitchen-window-request.model.ts`
- `equipment-type.model.ts` (string-literal union `'Treadmill' | 'IndoorBike' | 'BenchPress' | 'Elliptical'`)
- `session.model.ts`, `session-list-query.model.ts` (filter params), `create-session-request.model.ts`, `update-session-request.model.ts`
- `dashboard-summary.model.ts`
- `reward.model.ts`, `redemption-result.model.ts`, `tier.model.ts`
- `leaderboard-entry.model.ts`
- `validation-problem-details.model.ts` (matches the API's `ValidationProblemDetails` for client-side error mapping)

`API_BASE_URL` injection token (existing) is the only configurable knob. Bound from environment in `forge`.

## 3. Components library — reusable presentation only

Every component below is a `.ts`/`.html`/`.scss` triple under `projects/components/src/lib/<component>/`. No backend imports. Used by both `domain` and `forge`.

| Component                | Selector                | Purpose                                                                                                | L2s | Mock-derived |
|--------------------------|-------------------------|--------------------------------------------------------------------------------------------------------|-----|--------------|
| `CardComponent` (existing)| `forge-card`            | Shape: surface-container-lowest, M3 elevation 1, rounded `--shape-lg`, optional title.                 | shared | every authenticated screen |
| `ButtonComponent`        | `forge-button`          | Variants `filled`, `outlined`, `text`. Touch target ≥48dp. Accepts `[disabled]`, `[loading]`.          | L2-046 | every CTA |
| `IconButtonComponent`    | `forge-icon-button`     | 48×48dp circular. `<ng-content>` for the icon glyph.                                                   | L2-046 | top app bar, password reveal |
| `FieldComponent`         | `forge-field`           | M3 outlined text field anatomy: floating label, supporting text slot, error slot. Wraps a projected `<input>`/`<textarea>`/`<select>`. Accepts `[label]`, `[supportingText]`, `[error]`. | L2-005, L2-007 | sign-in, sign-up, profile, workout-detail |
| `CheckboxComponent`      | `forge-checkbox`        | M3 checkbox shape with `[checked]`/`(checkedChange)`.                                                  | L2-002 (Remember me) | sign-in |
| `SwitchComponent`        | `forge-switch`          | M3 switch for boolean toggles.                                                                          | L2-026, L2-027 | profile |
| `ChipComponent`          | `forge-chip`            | Filter / selection chip. `[selected]` modifier.                                                        | L2-008 | workouts |
| `ProgressRingComponent`  | `forge-progress-ring`   | SVG ring renderer driven by `[value]`/`[max]`. Used for the daily calorie ring + reward progress.       | L2-011 | dashboard, rewards |
| `BadgeComponent`         | `forge-badge`           | Pill-shaped status indicator. Variants: `success`, `warning`, `error`, `neutral`.                       | L2-022, L2-024 | dashboard tier, error-state diagnostics |
| `EmptyStateComponent`    | `forge-empty-state`     | Reusable empty-state shell: illustration slot, copy, primary CTA.                                       | L2-028 | empty-state, every list when zero rows |
| `ErrorBannerComponent`   | `forge-error-banner`    | Inline error banner with retry CTA.                                                                     | L2-024, L2-029 | error-state |
| `AppShellComponent`      | `forge-app-shell`       | Authenticated shell: top app bar with profile menu, projected `<router-outlet>`, bottom nav (XS/SM/MD) / nav rail (LG/XL). | L2-010, L2-013, L2-046 | dashboard, workouts, rewards, profile |
| `BottomNavComponent`     | `forge-bottom-nav`      | Mobile navigation strip. Used inside `AppShellComponent` at <992px.                                     | L2-013, L2-046 | every authed screen on mobile |
| `NavRailComponent`       | `forge-nav-rail`        | Desktop nav rail. Used inside `AppShellComponent` at ≥992px.                                            | L2-013 | every authed screen on desktop |

None of these reach for the backend. Every one is a pure presentation component driven by `@Input` / `@Output` / `<ng-content>`.

## 4. Domain library — components that consume `api` services

Every domain component injects `@Inject(*_SERVICE) interface` rather than the concrete class (interface-driven service consumption). One triple per component.

| Component                       | Selector                        | Consumes services (via tokens)                | Composes from `components`                       | L2s | Mock |
|---------------------------------|---------------------------------|------------------------------------------------|--------------------------------------------------|-----|------|
| `SignInFormComponent` (existing)| `forge-sign-in-form`            | `AUTH_SERVICE`                                  | `CardComponent`, `FieldComponent`, `CheckboxComponent`, `ButtonComponent` | L2-002 | sign-in |
| `SignUpFormComponent`           | `forge-sign-up-form`            | `AUTH_SERVICE`                                  | `CardComponent`, `FieldComponent`, `ButtonComponent` | L2-001 | sign-up |
| `PasswordResetRequestFormComponent` | `forge-password-reset-request-form` | `AUTH_SERVICE`                              | `CardComponent`, `FieldComponent`, `ButtonComponent` | L2-004 | password-reset |
| `PasswordResetConfirmFormComponent` | `forge-password-reset-confirm-form` | `AUTH_SERVICE`                              | `CardComponent`, `FieldComponent`, `ButtonComponent` | L2-004 | password-reset |
| `HealthBadgeComponent` (existing)| `forge-health-badge`           | `HEALTH_SERVICE`                                | `CardComponent`, `BadgeComponent`                  | L2-044 | error-state, dashboard |
| `DailyRingCardComponent`        | `forge-daily-ring-card`         | `DASHBOARD_SERVICE`                             | `CardComponent`, `ProgressRingComponent`           | L2-011, L2-012 | dashboard |
| `StreakCardComponent`           | `forge-streak-card`             | `DASHBOARD_SERVICE`                             | `CardComponent`, `BadgeComponent`                  | L2-013, L2-020 | dashboard |
| `WeightProgressCardComponent`   | `forge-weight-progress-card`    | `DASHBOARD_SERVICE`                             | `CardComponent`                                    | L2-014, L2-015 | dashboard |
| `TierCardComponent`             | `forge-tier-card`               | `REWARDS_SERVICE`, `DASHBOARD_SERVICE`         | `CardComponent`, `BadgeComponent`                  | L2-022 | dashboard, profile |
| `LeaderboardCardComponent`      | `forge-leaderboard-card`        | `LEADERBOARD_SERVICE`                           | `CardComponent`                                    | L2-027 | dashboard |
| `WorkoutListComponent`          | `forge-workout-list`            | `SESSIONS_SERVICE`, `EQUIPMENT_SERVICE`        | `CardComponent`, `ChipComponent`, `EmptyStateComponent`, `ButtonComponent` | L2-007, L2-008, L2-010, L2-028 | workouts, empty-state |
| `WorkoutDetailFormComponent`    | `forge-workout-detail-form`     | `SESSIONS_SERVICE`, `EQUIPMENT_SERVICE`        | `CardComponent`, `FieldComponent`, `ButtonComponent` | L2-007, L2-009, L2-010 | workout-detail |
| `WorkoutPointsBreakdownComponent` | `forge-workout-points-breakdown` | `SESSIONS_SERVICE`                            | `CardComponent`                                    | L2-018, L2-019, L2-020 | workout-detail |
| `RewardsCatalogComponent`       | `forge-rewards-catalog`         | `REWARDS_SERVICE`                               | `CardComponent`, `ButtonComponent`, `ProgressRingComponent` | L2-021 | rewards |
| `ProfileFormComponent`          | `forge-profile-form`            | `ME_SERVICE`, `PROFILE_SERVICE`                | `CardComponent`, `FieldComponent`, `SwitchComponent`, `ButtonComponent` | L2-005..L2-006, L2-014..L2-017, L2-026, L2-027 | profile |
| `SyncErrorPanelComponent`       | `forge-sync-error-panel`        | `HEALTHKIT_SERVICE`, `HEALTH_SERVICE`          | `CardComponent`, `ErrorBannerComponent`, `BadgeComponent`, `ButtonComponent` | L2-024, L2-029 | error-state |

## 5. Page-level shells — `forge` (host application)

Every page is a `*.page.ts` triple under `projects/forge/src/app/pages/<feature>/`. Pages compose domain + components only — they never call services directly.

| Route                              | Page component                  | Composition (domain + components)                                                                  | L2s | Mock |
|------------------------------------|---------------------------------|----------------------------------------------------------------------------------------------------|-----|------|
| `/` → `/dashboard`                 | redirect                        | n/a                                                                                                | n/a | n/a |
| `/sign-in`                         | `SignInPage` (existing)         | `SignInFormComponent`                                                                              | L2-002 | sign-in |
| `/sign-up`                         | `SignUpPage`                    | `SignUpFormComponent`                                                                              | L2-001 | sign-up |
| `/password-reset`                  | `PasswordResetPage`             | `PasswordResetRequestFormComponent`, `PasswordResetConfirmFormComponent` (token query param toggles) | L2-004 | password-reset |
| `/dashboard`                       | `DashboardPage` (rewrite)       | `AppShellComponent`, `DailyRingCardComponent`, `StreakCardComponent`, `WeightProgressCardComponent`, `TierCardComponent`, `LeaderboardCardComponent` | L2-011..L2-014, L2-022, L2-027 | dashboard |
| `/workouts`                        | `WorkoutsPage`                  | `AppShellComponent`, `WorkoutListComponent`                                                         | L2-007, L2-008, L2-010 | workouts |
| `/workouts/new`                    | `WorkoutNewPage`                | `AppShellComponent`, `WorkoutDetailFormComponent`                                                   | L2-007 | workout-detail (create variant) |
| `/workouts/:id`                    | `WorkoutDetailPage`             | `AppShellComponent`, `WorkoutDetailFormComponent`, `WorkoutPointsBreakdownComponent`                | L2-007, L2-009, L2-018..L2-020 | workout-detail |
| `/rewards`                         | `RewardsPage`                   | `AppShellComponent`, `TierCardComponent`, `RewardsCatalogComponent`                                 | L2-021, L2-022 | rewards |
| `/profile`                         | `ProfilePage`                   | `AppShellComponent`, `ProfileFormComponent`, `TierCardComponent`                                    | L2-005..L2-006, L2-014..L2-017, L2-022, L2-026, L2-027 | profile |
| `/error`                           | `ErrorPage`                     | `SyncErrorPanelComponent`                                                                            | L2-024, L2-029 | error-state |
| `**` (404)                         | `NotFoundPage`                  | `EmptyStateComponent` configured as 404                                                              | L2-029 | n/a |

The existing `AuthStateService` (in `forge`) plus `authInterceptor` (in `forge`) continue to own the access-token / refresh-token lifecycle. A new `authGuard` (in `forge`) protects every authenticated route and redirects unauthed visits to `/sign-in`.

`api` and `dashboard` services accept a `RetryStrategy` configured globally (HTTP interceptor in `forge`) — exponential backoff up to 3 attempts on 5xx; 4xx errors are surfaced to the page directly.

## 6. Design tokens

Lives in `projects/forge/src/styles.scss`. Every token is a CSS custom property under `:root` so every component, library, and page reads them via `var(--token)`. No SCSS variables for theme values — only CSS custom properties (so dynamic theme switching is possible without rebuild).

Color roles (from MF1, kept):

- `--md-sys-color-primary`, `--md-sys-color-on-primary`, `--md-sys-color-primary-container`, `--md-sys-color-on-primary-container`
- `--md-sys-color-secondary`, `--md-sys-color-on-secondary`, `--md-sys-color-secondary-container`, `--md-sys-color-on-secondary-container`
- `--md-sys-color-error`, `--md-sys-color-on-error`
- `--md-sys-color-background`, `--md-sys-color-on-background`
- `--md-sys-color-surface`, `--md-sys-color-on-surface`, `--md-sys-color-surface-container-lowest`, `--md-sys-color-surface-container-low`, `--md-sys-color-surface-container`, `--md-sys-color-on-surface-variant`, `--md-sys-color-outline`, `--md-sys-color-outline-variant`

New tokens (FP1 adds):

- Type scale: `--type-display-l`, `--type-display-m`, `--type-headline-l`, `--type-headline-m`, `--type-headline-s`, `--type-title-l`, `--type-title-m`, `--type-body-l`, `--type-body-m`, `--type-body-s`, `--type-label-l`, `--type-label-m`, `--type-label-s` — each defines `font-size` + `line-height` + `font-weight` + `letter-spacing` per Material 3 type scale.
- Elevation: `--elevation-1` through `--elevation-5` — box-shadow values matching M3 elevation tiers.
- Spacing: `--space-1` (4px) through `--space-12` (96px) — 4-pixel grid steps.
- Shape: `--shape-xs` (4px), `--shape-sm` (8px), `--shape-md` (12px), `--shape-lg` (16px), `--shape-xl` (28px), `--shape-full` (999px).
- Breakpoints (mirrored as SCSS variables in a tiny `_breakpoints.scss` shared file consumed via `@use`): `--breakpoint-xs` (0), `--breakpoint-sm` (576px), `--breakpoint-md` (768px), `--breakpoint-lg` (992px), `--breakpoint-xl` (1200px). L2-030 grid.

Dark theme tokens layered under `[data-theme="dark"]` selector (see Profile § L2-005 — system default for the MVP, manual toggle deferred).

## 7. Authentication integration

Local username/password sign-in with JWT bearer (Implementation Guidance — Authentication, frontend side; L2-002, L2-032, L2-033, L2-036). PKCE / external IdPs are explicitly out of scope.

- `AuthStateService` (existing in `forge`) holds access token + refresh token + `currentUser` in private signals; `snapshot()` exposes a read-only signal for templates.
- `authInterceptor` (existing in `forge`) attaches `Authorization: Bearer ${accessToken}` to every outgoing request whose URL starts with `API_BASE_URL`. Strips the header for cross-origin requests.
- New `refreshInterceptor` in `forge`: when a request returns `401` and a refresh token is available, calls `IAuthService.refresh(...)` once, retries the original request with the new access token, and emits the rotated tokens to `AuthStateService`. On refresh failure, calls `AuthStateService.clear()` and routes to `/sign-in`.
- New `authGuard` (`canActivate`) in `forge`: redirects to `/sign-in?returnUrl=...` when `AuthStateService.snapshot()` is null. Wired on every route except `/sign-in`, `/sign-up`, `/password-reset`, and `/error`.
- Sign-out flow: `IAuthService.signOut(refreshToken)` → revoke server-side, clear local state, route to `/sign-in`.
- Storage policy: tokens live in memory only — no `localStorage`, no `sessionStorage`. Hard refresh of the browser ends the session. (Future BI1+ work can introduce httpOnly refresh cookies if required; not in MVP scope.)

## 8. Playwright POM acceptance test inventory

Every important user flow gets a Playwright test under `frontend/tests/`. Each test header carries `// Traces to: <L2-IDs>` (Implementation Guidance — Testing). Page objects live under `frontend/tests/pom/`.

| Spec file                              | Page objects                                         | Flow covered                                                                                          | L2s |
|----------------------------------------|------------------------------------------------------|------------------------------------------------------------------------------------------------------|-----|
| `sign-in.spec.ts` (existing)           | `SignInPage`, `DashboardPage`                        | Sign in → land on dashboard, render greeting + health badge.                                          | L2-002, L2-013, L2-044 |
| `sign-up.spec.ts`                      | `SignUpPage`, `DashboardPage`                        | Register → auto sign-in → dashboard.                                                                   | L2-001 |
| `password-reset.spec.ts`               | `PasswordResetPage`, `SignInPage`                    | Request reset (always 202), confirm with token, sign in with new password.                            | L2-004 |
| `account-deletion.spec.ts`             | `ProfilePage`, `SignInPage`                          | Delete account → next sign-in attempt with same email returns 401.                                    | L2-006, L2-050 |
| `dashboard.spec.ts`                    | `DashboardPage`                                       | Dashboard renders calorie ring (980/1500), minutes tile (70 min), streak badge, tier, weight progress, leaderboard tile. | L2-011..L2-014, L2-022, L2-027 |
| `workouts-list.spec.ts`                | `WorkoutsPage`, `WorkoutDetailPage`                  | Navigate to workouts, filter by Treadmill chip, open a session, edit duration, save → ledger row updates. | L2-008, L2-009, L2-010, L2-018 |
| `workout-create.spec.ts`               | `WorkoutsPage`, `WorkoutNewPage`, `DashboardPage`    | Create a 22-minute treadmill session → dashboard ring updates within 1s.                              | L2-007, L2-011 |
| `workout-delete-refund.spec.ts`        | `WorkoutDetailPage`, `DashboardPage`                 | Create a session that earns +85 pts, delete it, dashboard balance returns to pre-session value.        | L2-009, L2-018..L2-020 |
| `rewards-redeem.spec.ts`               | `RewardsPage`                                         | With 1,250 pts, redeem a 500-pt reward → balance becomes 750. With insufficient balance → 400 banner. | L2-021 |
| `profile.spec.ts`                      | `ProfilePage`                                         | Edit first name, time zone, daily targets, morning + kitchen windows; reload — values persist.        | L2-005, L2-014..L2-017 |
| `responsive.spec.ts`                   | `DashboardPage`, `WorkoutsPage`, `ProfilePage`       | Snapshot each major page at 360 / 768 / 1440. Asserts no horizontal scroll, key elements visible, dashboard nav switches from bottom bar to nav rail at ≥1200px. | L2-030 |
| `accessibility.spec.ts`                | each page                                              | Run `@axe-core/playwright` against every authenticated route at WCAG 2.1 AA. Zero violations.         | L2-045 |
| `error-state.spec.ts`                  | `ErrorPage`                                           | Force a sync failure → user lands on `/error` with trace id, retry returns to `/dashboard`.          | L2-024, L2-029 |
| `empty-state.spec.ts`                  | `WorkoutsPage`, `RewardsPage`                        | Brand-new account with zero sessions → workouts page shows "No workouts yet" empty state with CTA.    | L2-028 |

Total: 14 specs. Every mock screen is asserted by at least one test; every UI L2 is covered.

## 9. Deferred integrations (frontend-side no-ops)

Frontend has fewer deferred integrations than backend. The ones called out by L2s + mocks:

- **Apple HealthKit ingest** (L2-023). The `HealthKitService` is a thin client over `POST /api/healthkit/ingest`. The actual HealthKit bridge — a native iOS shim or a WebKit interop — is **explicitly out of scope** for the web MVP. The `error-state.html` mock's "HealthKit authorization permission revoked" diagnostic is rendered against the backend's stubbed sync result; it visually matches the mock but does not trigger any real HealthKit call.
- **Push notifications for morning reminder + kitchen-closes nudge** (L2-025, L2-026). The frontend renders the toggles in `ProfileFormComponent` and persists them through `IProfileService`. Push delivery itself is backend-driven (BT-032 logs the intended action; production transport deferred).
- **Apple / Google sign-in buttons** (L2-036). **Out of scope by L2.** Confirmed absent from every mock and from this plan; no UI exists for them.

No frontend feature is implemented as a "logging no-op" — the frontend is a thin presenter for backend state.

## 10. Mock-to-screen-to-test traceability

| Mock                  | Page                         | Domain components                                                                                                    | Acceptance test                            |
|-----------------------|------------------------------|----------------------------------------------------------------------------------------------------------------------|--------------------------------------------|
| `index.html`          | n/a (mock-only nav)          | n/a                                                                                                                   | n/a                                        |
| `sign-in.html`        | `SignInPage`                 | `SignInFormComponent`                                                                                                  | `sign-in.spec.ts`                          |
| `sign-up.html`        | `SignUpPage`                 | `SignUpFormComponent`                                                                                                  | `sign-up.spec.ts`                          |
| `password-reset.html` | `PasswordResetPage`          | `PasswordResetRequestFormComponent`, `PasswordResetConfirmFormComponent`                                              | `password-reset.spec.ts`                   |
| `dashboard.html`      | `DashboardPage`              | `AppShellComponent`, `DailyRingCardComponent`, `StreakCardComponent`, `WeightProgressCardComponent`, `TierCardComponent`, `LeaderboardCardComponent` | `dashboard.spec.ts`                        |
| `workouts.html`       | `WorkoutsPage`               | `AppShellComponent`, `WorkoutListComponent`                                                                            | `workouts-list.spec.ts`                    |
| `workout-detail.html` | `WorkoutDetailPage`, `WorkoutNewPage` | `AppShellComponent`, `WorkoutDetailFormComponent`, `WorkoutPointsBreakdownComponent`                            | `workout-create.spec.ts`, `workout-delete-refund.spec.ts`, `workouts-list.spec.ts` |
| `rewards.html`        | `RewardsPage`                | `AppShellComponent`, `TierCardComponent`, `RewardsCatalogComponent`                                                    | `rewards-redeem.spec.ts`                   |
| `profile.html`        | `ProfilePage`                | `AppShellComponent`, `ProfileFormComponent`, `TierCardComponent`                                                       | `profile.spec.ts`, `account-deletion.spec.ts` |
| `empty-state.html`    | n/a (state of `WorkoutsPage` / `RewardsPage`) | `EmptyStateComponent` (in `components`)                                                              | `empty-state.spec.ts`                      |
| `error-state.html`    | `ErrorPage`                  | `SyncErrorPanelComponent`                                                                                              | `error-state.spec.ts`                      |

Every mock in `./docs/mocks/` is rendered by at least one page; every page has at least one acceptance test.

## 11. Slice sequencing for FT1 / FI1

Each implementation slice follows the per-slice ATDD loop (write test, implement, evaluate, mark done) defined in `FI1`. Sequenced for parallelism with the backend BI1 phases:

1. **FI1.1 (auth surfaces, parallel with BI1.1)** — sign-up form, password-reset request + confirm forms, `RefreshInterceptor`, `authGuard`. Specs: `sign-up.spec.ts`, `password-reset.spec.ts`. Adds `MeService` placeholder (read leg) so the dashboard can light up when BI1.2 lands.
2. **FI1.2 (app shell, parallel with BI1.2)** — `AppShellComponent`, `BottomNavComponent`, `NavRailComponent`, `responsive.spec.ts`. Pages get the shell wrapper.
3. **FI1.3 (dashboard, parallel with BI1.3)** — `DailyRingCardComponent`, `StreakCardComponent`, `WeightProgressCardComponent`, `TierCardComponent`, `LeaderboardCardComponent`, `DashboardPage` rewrite, `dashboard.spec.ts`.
4. **FI1.4 (workouts)** — `WorkoutListComponent`, `WorkoutDetailFormComponent`, `WorkoutPointsBreakdownComponent`, `WorkoutsPage`, `WorkoutNewPage`, `WorkoutDetailPage`, `workouts-list.spec.ts`, `workout-create.spec.ts`, `workout-delete-refund.spec.ts`.
5. **FI1.5 (rewards + profile)** — `RewardsCatalogComponent`, `ProfileFormComponent`, `RewardsPage`, `ProfilePage`, `rewards-redeem.spec.ts`, `profile.spec.ts`, `account-deletion.spec.ts`.
6. **FI1.6 (empty + error states)** — `EmptyStateComponent`, `ErrorBannerComponent`, `SyncErrorPanelComponent`, `ErrorPage`, `NotFoundPage`, `empty-state.spec.ts`, `error-state.spec.ts`.
7. **FI1.7 (a11y pass)** — `accessibility.spec.ts` running axe-core across every authenticated route, fix any violations.

Each FI1.x slice is one or two BI1 backend slices' worth of work and is sized for one to two loop iterations.

## 12. Verification matrix (UI L2s)

| L2     | Frontend artifact in this plan                                                                  |
|--------|--------------------------------------------------------------------------------------------------|
| L2-001 | `SignUpFormComponent` + `IAuthService.register` (§4, §2)                                         |
| L2-002 | `SignInFormComponent` (§4) — existing                                                            |
| L2-003 | `AuthStateService.signOut` + `IAuthService.signOut` (§7)                                         |
| L2-004 | `PasswordResetRequestFormComponent` + `PasswordResetConfirmFormComponent` (§4)                   |
| L2-005 | `ProfileFormComponent` + `IProfileService.updateProfile` (§4, §2)                                |
| L2-006 | `ProfileFormComponent` delete-account flow + `IMeService.delete()` (§4, §2)                      |
| L2-007 | `WorkoutDetailFormComponent` create variant (§4)                                                 |
| L2-008 | `WorkoutListComponent` with filter chips (§4)                                                    |
| L2-009 | `WorkoutDetailFormComponent` edit + duplicate + delete (§4)                                       |
| L2-010 | `IEquipmentService` populating the equipment dropdown in `WorkoutDetailFormComponent` (§2)        |
| L2-011 | `DailyRingCardComponent` (§4)                                                                     |
| L2-012 | `DailyRingCardComponent` minutes-today tile (§4)                                                  |
| L2-013 | `DashboardPage` composition (§5)                                                                  |
| L2-014 | `WeightProgressCardComponent` + `ProfileFormComponent` weight goal field (§4)                     |
| L2-015 | `ProfileFormComponent` current-weight field + `IProfileService.recordCurrentWeight` (§4, §2)      |
| L2-016 | `ProfileFormComponent` morning window inputs (§4)                                                 |
| L2-017 | `ProfileFormComponent` kitchen window inputs (§4)                                                 |
| L2-018 | `WorkoutPointsBreakdownComponent` (§4)                                                            |
| L2-019 | `WorkoutPointsBreakdownComponent` (§4)                                                            |
| L2-020 | `StreakCardComponent` + `WorkoutPointsBreakdownComponent` (§4)                                    |
| L2-021 | `RewardsCatalogComponent` + `IRewardsService.redeem` (§4, §2)                                     |
| L2-022 | `TierCardComponent` (§4)                                                                          |
| L2-023 | `IHealthKitService` (§2) + `SyncErrorPanelComponent` (§4)                                         |
| L2-024 | `SyncErrorPanelComponent` (§4)                                                                    |
| L2-025 | `ProfileFormComponent` reminder toggle + backend dispatcher (§9 — frontend defers transport)      |
| L2-026 | `ProfileFormComponent` kitchen-closes nudge toggle (§4)                                           |
| L2-027 | `LeaderboardCardComponent` + `ILeaderboardService` + `ProfileFormComponent` opt-in toggle (§4, §2)|
| L2-028 | `EmptyStateComponent` (§3)                                                                        |
| L2-029 | `ErrorBannerComponent` + `SyncErrorPanelComponent` + `ErrorPage` + `NotFoundPage` (§3, §5)        |
| L2-030 | Breakpoints + `AppShellComponent` switch from bottom-nav to nav-rail; `responsive.spec.ts` (§6, §8)|
| L2-036 | No PKCE / external IdP UI exists — confirmed in §7 and §9                                          |
| L2-042 | Bundle size budget in `angular.json` (`maximumWarning: 500kB / maximumError: 1MB`); Lighthouse run from CI checks FCP/TTI |
| L2-045 | `accessibility.spec.ts` (§8) — axe-core scan at AA                                                  |
| L2-046 | `ButtonComponent`, `IconButtonComponent`, `BottomNavComponent` enforce ≥48dp targets                |
| L2-047 | Tab order verified per page in the corresponding spec (`sign-in.spec.ts` already asserts tab order) |
| L2-048 | Color contrast checked via axe-core in `accessibility.spec.ts`                                       |
| L2-052 | Angular default interpolation HTML-encodes; `[innerHTML]` is forbidden by lint rule introduced here  |

Backend-only L2s (L2-031..L2-035, L2-037..L2-044 except L2-042, L2-049..L2-051, L2-053..L2-054) are out of scope for this plan and are addressed by `./docs/plans/backend.md`.
