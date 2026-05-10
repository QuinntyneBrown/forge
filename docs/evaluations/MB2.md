# MB2 — Evaluate backend MVP

## Pass 1 - findings

Walked the Implementation Evaluation Rubric (criteria 1–10) against the MB1 deliverable at commit `62492e5`. Scope per MB2: Backend, Validation, Authentication, Testing, and General sections of Implementation Guidance.

### Mechanical checks

- `grep -E "TODO|FIXME|XXX|HACK|NotImplementedException|throw new Error\("` over `backend/src` — zero matches (criterion 4 ✅).
- `grep -E "IRepository|IUnitOfWork|class.*Repository"` over `backend/src` — zero matches (criterion 6 ✅, no repository / unit-of-work abstractions).
- `grep -E "System\.ComponentModel\.DataAnnotations|\[Required\]|\[StringLength\]|\[MaxLength\]|\[MinLength\]|\[EmailAddress\]"` over `backend/src` — zero matches (Validation guidance ✅, no DataAnnotations on commands/DTOs).
- One-type-per-file scan: every `.cs` file under `backend/src` declares exactly one top-level type (criterion 5 ✅). `Program.cs` is the only file with `partial class Program {}` alongside top-level statements; that is the canonical .NET 6+ pattern for enabling integration tests (`WebApplicationFactory<Program>`) and is not a violation.
- `dotnet restore && dotnet build` from `backend/` — 4 projects built, 0 errors, 0 warnings (criterion 10 ✅).

### Structural checks (criteria 1, 6 — Guidance adherence + SOLID/CQS shape)

- Clean Architecture project split present: `Forge.Domain` (no deps) ← `Forge.Application` ← `Forge.Infrastructure` ← `Forge.Api`. Inward dependency direction is enforced by project references.
- MediatR wired up in `Forge.Application/DependencyInjection.cs` via `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`.
- `IAppDbContext` is the only seam handlers see; `AppDbContext : DbContext, IAppDbContext` is registered in Infrastructure as `services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>())`.
- Validators registered by assembly scan via `AddValidatorsFromAssembly(assembly)`; pipeline behavior `ValidationBehavior<TRequest, TResponse>` registered open-generic and runs validators before each handler.
- Sample slice is real: `RegisterCommand` / `SignInCommand` / `CreateSessionCommand` each have a colocated `AbstractValidator<T>`; `GetSessionByIdQuery` has a query handler that uses `IAppDbContext` directly.
- Controllers (`AuthController`, `SessionsController`, `HealthController`) only call `IMediator.Send(...)` and shape the HTTP response — no business logic in controllers.
- One ASP.NET Core controller (`SessionsController`) exercises the protected path with `[Authorize]`; `AuthController` is intentionally anonymous (register / sign-in are the entry points); `HealthController` is intentionally anonymous (per L2-044).

### Authentication checks

- Passwords stored as bcrypt hashes via `BCryptPasswordHasher` with work factor `12` — adequate cost per Implementation Guidance.
- JWT issuance via `JwtTokenIssuer` produces an HS256 token with explicit `iss`, `aud`, `sub`, `email`, `role`, `jti`, `nbf`, and `exp` claims.
- JWT bearer middleware in `Program.cs` enables `ValidateIssuer = true`, `ValidateAudience = true`, `ValidateLifetime = true`, `ValidateIssuerSigningKey = true`, `ClockSkew = 30s`. Invalid signature → 401, expired token → 401.
- No PKCE flow, no external IdP — confirmed by grep (`grep -i "pkce\|google\|apple\|github" backend/src` returns zero matches).
- Out-of-scope-for-MVP per L1 set: refresh tokens (BI1), failed-sign-in lockout (L2-034 implementation slice), audit logging (L2-035 implementation slice). Not blocking for MB2.

### Runtime checks (criteria 2, 10 — Requirements coverage in entirety + Build and run clean)

Started the API via `dotnet run --project src/Forge.Api` against `(localdb)\mssqllocaldb`. `EnsureCreated` provisioned the schema. Ran the full sample-slice round-trip:

- `POST /api/auth/register` with `{ email, firstName, lastName, password }` → `200 OK` with `{ accessToken, userId, email, role }`. JWT decoded contains the documented claims.
- `POST /api/auth/sign-in` with the same email + password → `200 OK` with a fresh JWT. Length 431 chars (HS256 / payload as expected).
- `POST /api/sessions` with the access token + a treadmill session → `201 Created`, `Location` header populated, body returns the new session id.
- `GET /api/sessions/{id}` with the access token → `200 OK`, body matches the persisted record (round-trips through HTTP → MediatR → EF Core → SQL Server LocalDB).
- `GET /health` (no auth) → `200 { "status": "Healthy" }`.
- `POST /api/sessions` without bearer token → `401`.
- `POST /api/auth/sign-in` with the wrong password → `401` `{ "title": "Invalid credentials.", "status": 401 }`.
- `POST /api/auth/register` with a duplicate email → `409` `{ "title": "Email already registered.", "status": 409 }`.

The following blocking findings were found:

### Finding 1 — Error responses use `application/json` instead of `application/problem+json`

L2-040 requires validation failures to respond `400` with `content-type: application/problem+json` and a `ValidationProblemDetails` body. `ExceptionHandlingMiddleware.Invoke` set `context.Response.ContentType = "application/problem+json"` *before* `WriteAsJsonAsync(value)`, but `WriteAsJsonAsync(value)` overrides the content type to `application/json` regardless. Observed at runtime via `curl -w "%{content_type}"` against:

- `POST /api/auth/register` with an invalid body → `400` with `Content-Type: application/json; charset=utf-8` (expected `application/problem+json`).
- `POST /api/auth/register` with a duplicate email → `409` with the same incorrect content-type.
- `POST /api/auth/sign-in` with bad credentials → `401` with the same incorrect content-type.

**Fix:** call the four-arg overload `WriteAsJsonAsync(value, options: null, contentType: "application/problem+json")` in all four catch blocks of `ExceptionHandlingMiddleware`. The pre-write `context.Response.ContentType =` lines become dead and are removed.

### Non-blocking observations

- The role claim in issued JWTs uses the long-form URI `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` because `JwtTokenIssuer` writes via `ClaimTypes.Role`. ASP.NET's bearer middleware maps it back to `[Authorize(Roles="…")]` correctly, so functionality is fine. If the frontend wants to read the role directly from the JWT payload it will need to look at the URI claim. The implementation slice that introduces RBAC consumption (BI1) can decide whether to add a short-form `role` claim alongside.
- ATDD evidence (rubric criterion 8) is intentionally deferred for the MVP — BT1 is the task that introduces the Playwright POM acceptance test for the sample slice. The MVP itself was not gated on a pre-existing acceptance test.
- Mobile-first / responsive (rubric criterion 9) is N/A for backend — that criterion applies to MF1/MF2 frontend evaluations.
- `appsettings.json` ships a placeholder `Jwt:SigningKey`. The runbook explicitly calls out replacing it before any non-development deployment. Acceptable for an MVP committed to a private repo.

## Pass 2 - findings

Re-walked the rubric after applying the Pass 1 fix at the middleware (`ExceptionHandlingMiddleware.cs` — all four catch blocks now call `WriteAsJsonAsync(value, options: null, contentType: ProblemJson)`; the redundant pre-write `context.Response.ContentType =` assignments were removed). Rebuilt and restarted the API.

- **Finding 1** — resolved. Re-issued the three failing requests:
  - `POST /api/auth/register` invalid → `400` with `Content-Type: application/problem+json`.
  - `POST /api/auth/register` duplicate → `409` with `Content-Type: application/problem+json`.
  - `POST /api/auth/sign-in` bad creds → `401` with `Content-Type: application/problem+json`.

Re-checked all rubric criteria; build remains clean (0 errors, 0 warnings). Sample slice still round-trips end to end. No new findings.

Pass 2 produces zero blocking findings. MB2 is complete.
