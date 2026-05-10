# Frontend runbook

This is the operating manual for the Forge Fit Angular MVP. It explains how to run the dev server, the library structure, and how to run the acceptance test.

## Run locally

Prerequisites: Node 22+ (Node 24 works with a CLI warning), the backend running on `https://localhost:5001` (see `docs/runbooks/backend.md`).

```powershell
cd C:\projects\forge\frontend
npm install
npx ng serve forge --port 4321
```

The dev server listens at `http://localhost:4321`. The first route redirects to `/sign-in`. Sign in with an account that exists in the backend database (or register one via `POST https://localhost:5001/api/auth/register`); on success the app navigates to `/dashboard`, which renders the user's email/role and a "Server status" badge fetched from `GET /health`.

To use a different backend host, edit the `API_BASE_URL` provider in `projects/forge/src/app/app.config.ts`.

## Library structure

The Angular workspace is split into three libraries plus the host application. Dependencies point inward — `app → domain → components / api`; `domain → api → nothing`; `components → nothing`.

```
frontend/
├── projects/
│   ├── api/         # backend-facing services + DTO models
│   ├── components/  # reusable presentation components, no backend
│   ├── domain/      # components that consume api services
│   └── forge/       # main application (the host)
└── tests/           # Playwright POM acceptance tests
```

| Library      | Imports                                | Owns                                                                                                    |
|--------------|----------------------------------------|----------------------------------------------------------------------------------------------------------|
| `api`        | nothing                                | `AuthService` + `auth.service.contract.ts` (interface + injection token), `HealthService` + `health.service.contract.ts`, DTO models (`AuthResult`, `SignInRequest`, `HealthStatus`), `API_BASE_URL` injection token. |
| `components` | nothing                                | Reusable presentation components with no backend dependency. Currently: `CardComponent` (`<forge-card>`).|
| `domain`     | `api`, `components`                    | UI components that consume `api` services via their `*.service.contract.ts` (interface-driven service consumption). Currently: `SignInFormComponent` (`<forge-sign-in-form>`) consuming `IAuthService`, `HealthBadgeComponent` (`<forge-health-badge>`) consuming `IHealthService`. |
| `forge` (app)| `api`, `components`, `domain`          | Routes, page-level shells, auth state, HTTP interceptor, DI registration of every concrete `api` service against its contract token. |

### Interface-driven service consumption

`AuthService` and `HealthService` each have a sibling `*.service.contract.ts` file that exports an interface (e.g. `IAuthService`) and an `InjectionToken` (e.g. `AUTH_SERVICE`). Domain components inject the token, never the concrete class:

```typescript
// projects/domain/src/lib/sign-in-form/sign-in-form.component.ts
constructor(
  private readonly fb: FormBuilder,
  @Inject(AUTH_SERVICE) private readonly auth: IAuthService
) { ... }
```

The host app binds the concrete implementation to the token in `app.config.ts`:

```typescript
{ provide: AUTH_SERVICE, useClass: AuthService }
```

This keeps `domain` decoupled from `api`'s implementation details — tests can swap in a fake by binding the same token to a different class.

### One-type-per-file

Every component is split into a `.ts` / `.html` / `.scss` triple in its own folder. No inline `template:` strings, no inline `styles:` arrays. Verify:

```powershell
Get-ChildItem -Recurse projects -Filter "*.component.ts" | ForEach-Object { Select-String -Path $_.FullName -Pattern "template:" }
```

(no matches expected.)

### BEM class names

Every CSS class in the workspace follows Block / Block__Element / Block--Modifier:

- `card`, `card__header`, `card__title`, `card__body`
- `sign-in-form`, `sign-in-form__field`, `sign-in-form__label`, `sign-in-form__input`, `sign-in-form__error`, `sign-in-form__submit`
- `health-badge`, `health-badge__dot`, `health-badge__label`, `health-badge--healthy`, `health-badge--unhealthy`
- `dashboard`, `dashboard__header`, `dashboard__title`, `dashboard__greeting`, `dashboard__sign-out`, `dashboard__grid`
- `sign-in-page`, `sign-in-page__hero`, `sign-in-page__brand-mark`, `sign-in-page__title`, `sign-in-page__subtitle`, `sign-in-page__card`

No utility-only classes from Tailwind / Bootstrap.

## Acceptance test (ATDD)

The Playwright POM acceptance test for the sample flow lives in `frontend/tests/`:

- `tests/pom/sign-in.page.ts` — page object for `/sign-in`
- `tests/pom/dashboard.page.ts` — page object for `/dashboard`
- `tests/sign-in.spec.ts` — the test (header comment traces it to `L2-002`, `L2-013`, `L2-044`)

Run it:

```powershell
# 1. Backend must already be running
cd C:\projects\forge\backend
dotnet run --project src/Forge.Api

# 2. Run the test (Playwright auto-starts the Angular dev server on port 4321)
cd C:\projects\forge\frontend
npx playwright install chromium     # one-time
npx playwright test
```

Expected output:

```
Running 1 test using 1 worker
  ✓  1 [chromium] › tests\sign-in.spec.ts:31:5 › signs in and renders backend data on the dashboard (1.6s)
  1 passed
```

The test:
1. Registers a fresh user via the backend (`POST /api/auth/register`) in `beforeAll`.
2. Loads `/sign-in` in Chromium.
3. Fills the email + password and submits the form. The form (in `domain`) calls `IAuthService.signIn(...)` (provided by `api` and bound by `app`) which POSTs to `/api/auth/sign-in`.
4. On success, navigates to `/dashboard`.
5. Asserts the dashboard greeting contains the registered email and the `User` role.
6. Asserts the health badge (also `domain`, consuming `IHealthService` from `api`) reads `Healthy` after a `GET /health` round-trip.

This single test exercises every layer the MVP claims: route, page shell, domain component, api service contract + concrete implementation, HTTP interceptor (auth token injection on subsequent requests), and the backend MVP itself.

## What's intentionally absent in MVP

- Refresh-token client wiring (waits for the backend refresh-token slice in BI1.1).
- Real Angular Material components — the MVP uses bespoke Material 3 styling via design tokens in `styles.scss`. Angular Material is installed and `mat.theme(...)` is wired; FI1 will swap our hand-rolled inputs / buttons for `<mat-form-field>` / `<button mat-flat-button>` etc. as the design library matures.
- Full sign-up / password-reset / profile / sessions screens (they live in BI1.x slices once BT2 approves the task list).
- Service worker / PWA shell.
- i18n.
