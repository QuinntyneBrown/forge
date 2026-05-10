# Backend runbook

This is the operating manual for the Forge backend MVP. It explains how to run the API locally, how the layers fit together, and what each piece is responsible for.

## Run locally

Prerequisites: .NET 9 SDK, SQL Server SqlExpress (ships with Visual Studio or via `SqlSqlExpress.msi`), and the EF Core CLI tools (`dotnet tool install --global dotnet-ef` if not already installed).

```powershell
cd C:\projects\forge\backend
dotnet build
dotnet run --project src/Forge.Api
```

The API listens on `https://localhost:5001` and `http://localhost:5000`. Swagger UI is mounted at `/swagger` in Development. On startup, the API executes `Database.MigrateAsync()` against the configured SQL Server instance (e.g., `.\SQLEXPRESS` or `.\SQLEXPRESS`) and brings the `Forge` database up to the latest migration. Migrations live in `src/Forge.Infrastructure/Migrations/`.

To apply migrations manually outside an API run (e.g., in CI / deploy):

```powershell
dotnet ef database update --project src/Forge.Infrastructure --startup-project src/Forge.Api
```

To add a new migration after editing the `AppDbContext`:

```powershell
dotnet ef migrations add <MigrationName> --project src/Forge.Infrastructure --startup-project src/Forge.Api --output-dir Migrations
```

To use a different database, edit `ConnectionStrings:DefaultConnection` in `src/Forge.Api/appsettings.json` (or override via `ASPNETCORE_` environment variables).

Replace `Jwt:SigningKey` in `appsettings.json` with a per-environment secret of at least 32 characters before any non-development deployment.

## Smoke test

```powershell
# Register
$body = @{ email='quinn@forgefit.app'; firstName='Quinn'; lastName='B'; password='ForgeFit!2026' } | ConvertTo-Json
$reg = Invoke-RestMethod -Uri https://localhost:5001/api/auth/register -Method Post -Body $body -ContentType application/json

# Sign in
$sin = Invoke-RestMethod -Uri https://localhost:5001/api/auth/sign-in -Method Post -Body (@{ email='quinn@forgefit.app'; password='ForgeFit!2026' } | ConvertTo-Json) -ContentType application/json

# Create a session
$auth = @{ Authorization = "Bearer $($sin.accessToken)" }
$session = @{ equipment=1; startedAt='2026-05-10T05:12:00-04:00'; durationMinutes=22; distanceMiles=2.1; avgHeartRateBpm=128; activeCalories=218; notes='Easy zone 2' } | ConvertTo-Json
$created = Invoke-RestMethod -Uri https://localhost:5001/api/sessions -Method Post -Body $session -Headers $auth -ContentType application/json

# Read it back
Invoke-RestMethod -Uri "https://localhost:5001/api/sessions/$($created.id)" -Headers $auth
```

## Layered pattern

Clean Architecture, four projects, dependencies pointing inward:

| Project              | Depends on                       | Owns |
|----------------------|----------------------------------|------|
| `Forge.Domain`       | nothing                          | Aggregates and value types: `User`, `WorkoutSession`, `EquipmentType`. No EF, no MediatR, no HTTP. |
| `Forge.Application`  | `Forge.Domain`                   | Commands, queries, handlers, validators, MediatR pipeline behaviors, abstractions (`IAppDbContext`, `IPasswordHasher`, `IJwtTokenIssuer`, `ICurrentUser`). |
| `Forge.Infrastructure` | `Forge.Application`            | Concrete `AppDbContext : DbContext, IAppDbContext`, `BCryptPasswordHasher`, `JwtTokenIssuer`, `HttpContextCurrentUser`, EF Core registration. |
| `Forge.Api`          | `Forge.Application`, `Forge.Infrastructure` | ASP.NET Core controllers, JWT bearer authentication, exception-handling middleware, Swagger, composition root in `Program.cs`. |

Rules:

- **CQS via MediatR.** Every controller action sends a `Command` or `Query` through `IMediator`. There is no service layer between controller and handler.
- **No repositories, no unit-of-work.** Handlers inject `IAppDbContext` and use EF Core directly.
- **Validation runs in a pipeline behavior.** `ValidationBehavior<TRequest, TResponse>` invokes every `AbstractValidator<TRequest>` registered in the assembly before the handler executes; failures throw `ValidationException`, which `ExceptionHandlingMiddleware` maps to HTTP 400 `application/problem+json`.
- **One type per file.** Every `.cs` file contains exactly one top-level type whose name matches the file. New code must follow this — no combined files.
- **Authentication.** Local username + password only. Passwords stored as bcrypt hashes (work factor 12). Sign-in issues an HS256 JWT with `iss`, `aud`, `sub`, `email`, `role`, `jti`, `nbf`, `exp` claims; the JWT bearer middleware validates issuer, audience, signature, and expiration on every request. PKCE and external IdPs are out of scope.
- **Migrations.** EF Core `Database.MigrateAsync()` runs on app startup. Tables created by the initial migration `Initial_AuthAndSessions`: `Users`, `WorkoutSessions`, `RefreshTokens`, `SignInAttempts`, `AuditLogs`, `PasswordResetTokens`. Subsequent slices add tables / columns via additional migrations.

## Sample slice

`POST /api/auth/register` → `RegisterCommand` → `RegisterCommandValidator` → `RegisterCommandHandler` → bcrypt hash → `AppDbContext.Users.Add` → JWT issued.

`POST /api/auth/sign-in` → `SignInCommand` → validator → handler → bcrypt verify → JWT issued.

`POST /api/sessions` (Authorize) → `CreateSessionCommand` → `CreateSessionCommandValidator` → handler reads `ICurrentUser.UserId` from the JWT → `AppDbContext.WorkoutSessions.Add` → SQL Server.

`GET /api/sessions/{id}` (Authorize) → `GetSessionByIdQuery` → handler → `AppDbContext.WorkoutSessions` projection → `SessionDto`.

`GET /health` → returns `200 { "status": "Healthy" }` when the database is reachable, `503` otherwise. No authentication required.

## What's intentionally absent in MVP

- Refresh tokens (will arrive with the auth slice in `BI1`).
- Account lockout / rate-limiting (`L2-034` — implementation slice).
- Audit logging (`L2-035` — implementation slice).
- Transactional email (deferred per L2-004 — will use a logging no-op service when introduced).
- EF Core migrations: MVP uses `EnsureCreated`. Migrations will be added when the schema starts changing.
