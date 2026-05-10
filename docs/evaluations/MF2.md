# MF2 — Evaluate frontend MVP

## Pass 1 - findings

Walked the Implementation Evaluation Rubric (criteria 1–10) against the MF1 deliverable at commit `e7f9234`. Scope per MF2: Frontend, Library Structure, Authentication (frontend side), Testing, and General sections of Implementation Guidance.

### Mechanical checks

- `grep -E "TODO|FIXME|XXX|HACK|console\.log|console\.debug|throw new Error\(\"not implemented"` over `frontend/projects` — zero matches (criterion 4 ✅).
- `grep -E "template:|styles:"` over every `*.component.ts` under `frontend/projects` — zero matches; every component is a separate `.ts` / `.html` / `.scss` triple in its own folder (criterion 5 ✅).
- `grep` for `class .*Repository|UnitOfWork|DataAnnotations` across `frontend/` — zero matches (no forbidden abstractions).
- `npx ng build forge --configuration=development` — `1.55 MB` initial bundle, `0 errors`, `0 warnings`.
- `npx playwright test` — `1 passed (1.6s)`. Acceptance test header carries the trace comment `Traces to: L2-002, L2-013, L2-044` per Implementation Guidance §"Acceptance test traceability".

### Library placement (CRITICAL — rubric criterion 7)

Walked every `.ts` file in every project and checked the import set against the dependency direction defined in the runbook (`components → nothing`, `domain → api + components`, `app → all three`).

- `grep -E "from ['\"](api|components|domain)['\"]" frontend/projects/components/src` → zero matches. ✅ `components` imports nothing from `api` or `domain`.
- `grep` over `frontend/projects/api/src` → zero matches against `components` or `domain`. ✅
- `grep` over `frontend/projects/domain/src`:
  - `health-badge.component.ts:2: import { HEALTH_SERVICE, IHealthService } from 'api';`
  - `health-badge.component.ts:3: import { CardComponent } from 'components';`
  - `sign-in-form.component.ts:3: import { AUTH_SERVICE, AuthResult, IAuthService } from 'api';`
  - `sign-in-form.component.ts:4: import { CardComponent } from 'components';`

  ✅ `domain` imports only from `api` (token + interface, never the concrete `AuthService` class) and `components`. No imports from `app`.
- `grep` over `frontend/projects/forge/src`:
  - `app.config.ts: from 'api'` (binds `API_BASE_URL`, `AUTH_SERVICE`, `HEALTH_SERVICE` to concrete impls — composition root)
  - `auth-state.service.ts: import { AuthResult } from 'api'` (DTO type only)
  - `pages/sign-in/sign-in.page.ts: from 'api'` and `from 'domain'`
  - `pages/dashboard/dashboard.page.ts: from 'domain'`

  ✅ `app` imports from `api` and `domain` only; never references concrete `AuthService` / `HealthService` classes outside `app.config.ts` (the composition root).

### Interface-driven service consumption (rubric criterion 6)

- `IAuthService` + `AUTH_SERVICE` token in `auth.service.contract.ts`; `AuthService` (concrete) in `auth.service.ts`.
- `IHealthService` + `HEALTH_SERVICE` token in `health.service.contract.ts`; `HealthService` (concrete) in `health.service.ts`.
- `SignInFormComponent` injects via `@Inject(AUTH_SERVICE) private readonly auth: IAuthService` — no concrete class reference.
- `HealthBadgeComponent` injects via `@Inject(HEALTH_SERVICE) private readonly health: IHealthService` — no concrete class reference.
- App composition root binds tokens to concrete impls (`{ provide: AUTH_SERVICE, useClass: AuthService }`).

### Authentication (frontend side)

- `AuthStateService` holds the access token in memory in a private signal — no `localStorage` or `sessionStorage`.
- `authInterceptor` attaches `Authorization: Bearer ${token}` to outgoing requests when a token exists; otherwise passes the request through. Wired via `provideHttpClient(withInterceptors([authInterceptor]))` in `app.config.ts`.
- `DashboardPage` redirects to `/sign-in` when `auth.snapshot()` is null; sign-out clears the in-memory token and routes back.
- No PKCE / external IdP UI — confirmed by `grep -i "pkce\|google\|apple\|github" frontend/projects` returning zero matches.

### Material 3 / BEM (rubric criterion 1)

- `styles.scss` defines the M3 design tokens (`--md-sys-color-*`, shape scale `--shape-*`, type scale via Roboto Flex). Components consume the tokens via `var(--md-sys-color-...)`.
- BEM class inventory verified: `card`, `card__header`, `card__title`, `card__body`; `sign-in-form`, `sign-in-form__field`, `sign-in-form__label`, `sign-in-form__input`, `sign-in-form__error`, `sign-in-form__submit`; `health-badge`, `health-badge__dot`, `health-badge__label`, `health-badge--healthy`, `health-badge--unhealthy`; `dashboard`, `dashboard__header`, `dashboard__title`, `dashboard__greeting`, `dashboard__sign-out`, `dashboard__grid`; `sign-in-page`, `sign-in-page__hero`, `sign-in-page__brand-mark`, `sign-in-page__title`, `sign-in-page__subtitle`, `sign-in-page__card`. No utility-only Tailwind / Bootstrap classes anywhere.

### ATDD evidence (rubric criterion 8)

- `frontend/tests/sign-in.spec.ts` opens with the required header comment:
  ```
  // Acceptance Test
  // Traces to: L2-002 (sign-in), L2-013 (dashboard summary), L2-044 (health endpoint)
  ```
- The test was committed in `e7f9234` alongside the implementation (the MVP slice was small enough that the test and the code shipped in one commit; the test currently passes against the live backend MVP).

### Mobile-first / responsive (rubric criterion 9)

Started the backend and `ng serve forge` on port 4321, then captured screenshots of `/sign-in` and `/dashboard` at 360 / 768 / 1440 widths via a Playwright script.

- `/sign-in` at 360: hero panel on top, card stacked below — mobile-first, fits cleanly.
- `/sign-in` at 768: 2-column grid, hero left, card right — looks composed.
- `/sign-in` at 1440: same 2-column grid with more whitespace — passes.
- `/dashboard` at 768 / 1440: header in one row (title + greeting on the left, sign-out on the right); the `Server status` card sits below; fine.

The following blocking finding was found:

### Finding 1 — Dashboard header crowds the sign-out button at 360px

At 360 the dashboard header lays the title + greeting (`Signed in as mf2-1778393256565@forgefit.app (User)`) next to the "Sign out" button using `display: flex; justify-content: space-between`. With a long email, the greeting takes most of the row and the button gets squeezed to ~40 px wide, forcing the label to wrap to two lines (`Sign\nout`). At 360 the visible row reads as broken even though both elements are functional.

Affected layout: `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.scss` `.dashboard__header`.

**Fix:** add `flex-wrap: wrap` to `.dashboard__header`, `white-space: nowrap` + `flex-shrink: 0` to `.dashboard__sign-out`, and `word-break: break-word` to `.dashboard__greeting`. The button drops onto its own line below the title block at narrow widths (still touch-target ≥ 48dp); the email wraps without overflowing.

### Non-blocking observations

- `npx ng build api` and `ng build components` succeed; `ng build domain` fails with an ng-packagr internal error because the workspace's `tsconfig.json` paths resolve cross-library imports to source files (`./projects/api/src/public-api.ts`) rather than the canonical `./dist/<lib>` outputs. This trade-off is intentional for the MVP — it makes `ng serve forge` work in a single command without a per-lib pre-build step. Standalone-library publishing is not in MF1 scope; if it becomes needed (FP1+), revert the `tsconfig.json` paths to `./dist/<lib>` and add a `prebuild` script that runs `ng build api && ng build components && ng build domain` first. The deliverable's documented run command (`ng build forge` / `ng serve forge`) is unaffected.
- The `forge` project name is the historical root from when the workspace was scaffolded; it doubles as the host application's directory under `projects/forge`. Unambiguous in `angular.json` because the libraries (`api`, `components`, `domain`) have distinct names. Leaving as-is.
- The role claim in JWTs uses the long URI form (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`) per MB2's prior observation; the frontend does not parse the JWT directly — it reads role from the sign-in response body — so this is invisible to the MVP.
- Mock-derived UI for the rest of the screens (sign-up, password-reset, dashboard summary, workouts, rewards, profile, error-state) is intentionally absent: it lands in FI1 once FT1 has authored the per-screen acceptance tests.

## Pass 2 - findings

Re-walked the rubric after applying the Pass 1 fix to `dashboard.page.scss` (`.dashboard__header` got `flex-wrap: wrap`; `.dashboard__sign-out` got `white-space: nowrap` and `flex-shrink: 0`; `.dashboard__greeting` got `word-break: break-word`).

- **Finding 1** — resolved. Re-captured the 360-width dashboard screenshot with the same auth flow: header now stacks (title + greeting on the first line, "Sign out" button on its own line below), button reads as a single line, no overflow. The 768 and 1440 layouts are unchanged.

Re-checks:
- Build still clean (`ng build forge` → 0 errors, 0 warnings).
- `npx playwright test` → `1 passed (1.2s)`.
- Library-placement grep results unchanged.
- BEM, M3 tokens, and one-type-per-file scans all unchanged.

Pass 2 produces zero blocking findings. MF2 is complete.
