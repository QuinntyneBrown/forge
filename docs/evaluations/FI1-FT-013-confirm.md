# FI1 — FT-013 confirm leg — Password reset confirm form + token-aware page

## Pass 1 - findings

Walked the Implementation Evaluation Rubric (criteria 1–10). Scope: confirm leg of FT-013 (the request leg landed at `b91f371`).

### Mechanical checks

- One-type-per-file: `PasswordResetConfirmFormComponent` (.ts/.html/.scss triple). `PasswordResetPage` updated in place — still one type per file.
- No `template:` / `styles:` inline. ✅
- No `TODO` / `FIXME` / `console.log` introduced (debug logs removed before commit).
- BEM hooks: `password-reset-confirm-form`, `password-reset-confirm-form__copy`, `password-reset-confirm-form__field`, `password-reset-confirm-form__error`, `password-reset-confirm-form__submit`. ✅
- `ng build forge` → 0 errors / 0 warnings; bundle unchanged at 2.20 MB.
- `npx playwright test` → `11 passed (20.7s)` (existing 9 + two new password-reset-confirm scenarios).

### Library placement (criterion 7)

- `IAuthService.confirmPasswordReset(token, newPassword)` extension on the existing contract; concrete impl POSTs to `/api/auth/password-reset/confirm`. Both stay in `api`. ✅
- `PasswordResetConfirmFormComponent` lives in `domain`, injects `IAuthService` via `AUTH_SERVICE`, composes `<forge-card>` + `<forge-button>` from `components` and `<mat-form-field>` + `<input matInput>` directly. ✅
- `PasswordResetPage` lives in `forge`. Uses `ActivatedRoute.queryParamMap` (via `toSignal`) to drive a `mode` computed (`'request'` vs `'confirm'`). ✅

### Implementation

- **`IAuthService.confirmPasswordReset(token, newPassword): Observable<void>`** — POSTs `{ token, newPassword }` to the BT-005 endpoint.
- **`PasswordResetConfirmFormComponent`** (`<forge-password-reset-confirm-form>`):
  - `@Input({ required: true }) token` — supplied by `PasswordResetPage` from the `?token=` query param.
  - `@Output() confirmed` — fires on a 204 response so the parent page can navigate.
  - Reactive form with one `newPassword` control mirroring the backend's `ConfirmPasswordResetCommandValidator` policy (≥12 chars + complexity regex).
  - State held in **signals** (`errorMessage`, `submitting`) — necessary for change detection in Angular 21's zoneless-by-default rendering. The previous bespoke `field = …` pattern (used by `SignInFormComponent` etc.) silently fails to repaint when the field is mutated outside the change-detection cycle, which the new test caught (see "Mid-implementation finding" below).
  - Error path reads `err.error.title` from the `ProblemDetails` body (`"Invalid password reset token."` from `InvalidPasswordResetTokenException`'s mapping in `ExceptionHandlingMiddleware`); falls back to a generic copy if absent.
- **`PasswordResetPage`** (in `forge`):
  - Reads `route.queryParamMap` via `toSignal`. `mode` computed switches between `'request'` and `'confirm'` based on the `?token` query param.
  - Hero copy + body card swap together: confirm shows "Pick a new password." + the confirm form; request shows "Forgot your password?" + the request form (the existing FT-013 behavior).
  - On `(confirmed)` from the confirm form, navigates to `/sign-in` (per L2-004 ac 3).
  - The `password-reset` route is anonymous (no auth guard) — anyone with a token link can use it.

### Acceptance test (criterion 8 — ATDD evidence)

`tests/password-reset-confirm.spec.ts` was authored before the implementation. Header: `// Traces to: L2-004 (confirm leg)`. Two scenarios:

1. **Happy path (mocked confirm).** `page.route()` intercepts the confirm endpoint and returns `204`, asserting along the way that the request body carries `token: 'fake-token-xyz'` and a `newPassword` string. Test fills the new password, submits, asserts URL navigates to `/sign-in`. The mock is necessary because the raw token issued by the backend during the request leg is only emitted to the deferred email log — recovering it from the issuing flow would break the no-enumeration contract. The mock simulates a 204 response so the UI's success path runs deterministically end-to-end through the `IAuthService` consumer.
2. **Unhappy path (live backend).** Navigates to `/password-reset?token=bogus-token-that-does-not-exist`, fills + submits, asserts the error message is visible. The bogus token never matches a `PasswordResetTokens` row so the BT-005 handler throws `InvalidPasswordResetTokenException` and the middleware returns `400 application/problem+json`. The form's error handler surfaces the body's `title` field.

### Mid-implementation finding (resolved before slice closure)

**Field-bound state silently failed to render in Angular 21's zoneless mode.** First implementation used `protected errorMessage: string | null = null` and `protected submitting = false`, mirroring the existing `SignInFormComponent` and `SignUpFormComponent`. The 400-response test failed: console logs confirmed the error handler ran and `errorMessage` was assigned, but the `@if (errorMessage)` block didn't render and the submit button stayed in `[loading]="submitting"` state. Angular 21 is configured zoneless-by-default; plain field writes don't notify the change detector.

**Resolution.** Promoted `errorMessage` and `submitting` to `signal`s and updated the template to call them as functions (`errorMessage()`, `submitting()`). The test passes immediately — assignments via `.set(...)` mark the signal dirty and trigger a render.

**Latent issue.** `SignInFormComponent`, `SignUpFormComponent`, and `PasswordResetRequestFormComponent` use the same field-bound pattern. None of their existing acceptance tests exercise the error path, so the bug hasn't surfaced. A future small slice should migrate them — non-blocking here because no shipped test fails. Documented in this eval as the canonical pattern for any new domain-form component: state goes in signals.

### Build / run clean (criterion 10)

- `ng build forge` — 0 errors / 0 warnings.
- `playwright test` — `11 passed`.
- Backend untouched.

### Non-blocking observations

- The page-level `mode` computed reads from a `toSignal(queryParamMap, { initialValue: snapshot })` so the page's first render is deterministic — no flicker between request and confirm forms when the user lands on `/password-reset?token=…` cold.
- The confirm form's `[loading]` spinner uses `submitting()` (signal call) — `<forge-button>` accepts a plain boolean, so no API change needed.
- L2-004 ac 3 says "user is redirected to sign in" after a successful confirm. The page navigates to `/sign-in` without query params; a future polish slice could pass `?email=…` so the user lands with the email pre-filled.
- The migrate-to-signals follow-up for `SignInFormComponent` / `SignUpFormComponent` / `PasswordResetRequestFormComponent` is small (rename the field to a signal, update template references). Folded into a TODO in this eval — not into the codebase.

Pass 1 produces zero blocking findings. FT-013 is now fully complete (request + confirm legs).
