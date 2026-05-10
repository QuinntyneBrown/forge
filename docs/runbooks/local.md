# Local runbook

The single command to bring up ForgeFit on a fresh clone.

## Prerequisites

- **.NET 9 SDK** — `dotnet --version` reports 9.x.
- **Node 20+** — `node --version` reports 20.x or 22.x.
- **SQL Server** — either SQL Server Express (Windows, default) or LocalDB (Visual Studio install). The `appsettings.json` connection string targets `Server=.\SQLEXPRESS;Database=Forge;…`. Edit that file or set the `ConnectionStrings__DefaultConnection` env var to point elsewhere.
- **EF Core CLI** (only if you'll add migrations) — `dotnet tool install --global dotnet-ef`.

## The single command

### Windows / PowerShell

```powershell
./scripts/start-local.ps1
```

### macOS / Linux / Git Bash on Windows

```bash
./scripts/start-local.sh
```

Both scripts:

1. Launch the backend (`dotnet run --project backend/src/Forge.Api`). The API listens on `https://localhost:5001` (and `http://localhost:5000`). Migrations apply on boot via `Database.MigrateAsync()`.
2. Poll `GET /health` for up to two minutes until the API responds.
3. `npm install` in `frontend/` (first run only).
4. Start `npx ng serve forge --port 4321`.

The frontend is reachable at <http://localhost:4321>. Sign in with an account you've registered (or `POST /api/auth/register` first), and the dashboard renders.

Pass an alternate port if 4321 is taken:

```powershell
./scripts/start-local.ps1 -FrontendPort 4200
```

```bash
./scripts/start-local.sh 4200
```

## Smoke check

A fresh clone is "green" when:

```bash
git clone <repo>
cd forge
./scripts/start-local.sh        # or .ps1 on Windows
# (waits for "Backend ready" then ng serve attaches)
```

…and visiting <http://localhost:4321/sign-up> renders the sign-up form, completing it routes to `/dashboard`, and the dashboard's tier card reads `Iron / 0 pts available`.

## Component-level detail

For deeper context on each side, see:

- [`docs/runbooks/backend.md`](backend.md) — Clean Architecture layers, smoke-test PowerShell snippets, migration commands, sample slice walk-through.
- [`docs/runbooks/frontend.md`](frontend.md) — Library layout (`api` / `components` / `domain` / `forge`), interface-driven service consumption pattern, Playwright test layout.

## Stopping

- **PowerShell** — Ctrl+C in the foreground frontend window stops `ng serve`. The backend stays open in the spawned PowerShell window — close it (or `Stop-Process -Name dotnet`) when done.
- **bash** — Ctrl+C kills both processes (the script traps `EXIT/INT/TERM` and signals the backend).

## Troubleshooting

| Symptom                                                  | Likely cause                                                                                                                                                              |
|----------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Backend did not respond within 2 minutes` (bash script) | SQL Server not reachable. Check `/tmp/forge-api.log`; common fixes: start SQL Server service, verify `appsettings.json` connection string, run `sqlcmd -S .\SQLEXPRESS`. |
| Frontend reports `https://localhost:5001` CORS error     | Backend started on a non-default port. Edit `API_BASE_URL` provider in `frontend/projects/forge/src/app/app.config.ts`.                                                   |
| `port 4321 in use`                                       | Pass `-FrontendPort 4200` (PowerShell) or `4200` as the first arg (bash).                                                                                                 |
| Browser warns about self-signed cert on `https://localhost:5001` | Trust the dev cert: `dotnet dev-certs https --trust`.                                                                                                              |
