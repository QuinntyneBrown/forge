[![CI](https://github.com/QuinntyneBrown/forge/actions/workflows/ci.yml/badge.svg)](https://github.com/QuinntyneBrown/forge/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# Forge Fit

Forge Fit is a full-stack fitness gamification application for building better morning workout habits, discouraging late-night eating, and rewarding consistency over time.

The project combines a .NET 9 backend with an Angular 21 frontend and is organized as a multi-project workspace with acceptance-test coverage, responsive UI patterns, and a documented product/design trail under `docs/`.

## Features

### Implemented today

- JWT-based authentication with registration, sign-in, refresh, sign-out, password reset request, and password reset confirmation
- profile management with editable personal details and daily target settings
- dashboard summary and current-user lookups
- workout session APIs for create, list, update, duplicate, delete, and detail retrieval
- rewards and tier APIs, plus leaderboard and equipment endpoints
- HealthKit ingest endpoint and notification pipeline seams
- Angular application flows for sign-in, sign-up, password reset, dashboard access control, and profile editing
- reusable frontend `api`, `components`, and `domain` libraries
- Playwright end-to-end tests and .NET acceptance tests

### In progress

- the Angular UI currently implements authentication, dashboard, and profile flows first; additional workout and rewards screens are scaffolded in the product/design docs and backend APIs

## Tech Stack

| Area | Technology |
| --- | --- |
| Backend | .NET 9, ASP.NET Core, MediatR, FluentValidation, Entity Framework Core, SQL Server |
| Frontend | Angular 21, Angular Material, SCSS |
| Testing | xUnit, ASP.NET Core integration/acceptance tests, Playwright |
| Tooling | npm, Angular CLI, GitHub Actions |

## Architecture

### Backend

The backend follows a clean architecture layout under `backend/src`:

- `Forge.Api` - ASP.NET Core host, controllers, middleware, auth, Swagger
- `Forge.Application` - commands, queries, validators, pipeline behaviors
- `Forge.Domain` - core entities and enums
- `Forge.Infrastructure` - EF Core, SQL Server, JWT issuing, password hashing, deferred integrations

Acceptance tests live under `backend/tests/Forge.Acceptance`.

### Frontend

The frontend is an Angular workspace under `frontend/` with:

- `projects/forge` - the main application
- `projects/api` - backend models, tokens, and API services
- `projects/components` - reusable presentational components
- `projects/domain` - feature-oriented UI components built on the API layer
- `e2e/tests` - Playwright end-to-end specs
- `e2e/pages` - Playwright page objects and test helpers

Routing currently covers:

- `/sign-in`
- `/sign-up`
- `/password-reset`
- `/dashboard`
- `/profile`

## Repository Layout

```text
backend/   .NET solution, API, application, domain, infrastructure, tests
frontend/  Angular workspace, app, libraries, Playwright E2E suite
docs/      Product brief, specs, static mocks, and screenshots
scripts/   Local startup and provisioning scripts
```

## Getting Started

### Prerequisites

- .NET SDK 9.0.x
- Node.js 20+
- npm
- SQL Server instance reachable by the backend connection string (e.g., SQLEXPRESS or LocalDB)

### Backend

1. Restore dependencies:

   ```bash
   cd backend
   dotnet restore
   ```

2. Update `backend/src/Forge.Api/appsettings.json` with a real JWT signing key and, if needed, a different SQL Server connection string.

3. Start the API:

   ```bash
   dotnet run --project src/Forge.Api
   ```

The API applies EF Core migrations on startup. By default it uses:

- API base URL: `https://localhost:5001`
- database: `Server=.\SQLEXPRESS;Database=Forge;Trusted_Connection=True;TrustServerCertificate=True` (or `(localdb)\mssqllocaldb` if available)

### Frontend

1. Install dependencies:

   ```bash
   cd frontend
   npm ci
   ```

2. Start the Angular app:

   ```bash
   npm start
   ```

The default local frontend URL is `http://localhost:4200`.

## Development Workflow

### Backend commands

```bash
cd backend
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

### Frontend commands

```bash
cd frontend
npm run build
npm run e2e
```

### Design mock workflow

Static product mocks and generated screenshots live under `docs/mocks`. Open `docs/mocks/index.html` to review the current mock set.

## Quality and CI

GitHub Actions runs:

- backend restore, build, test, and vulnerable-package scanning
- frontend install, build, and production dependency audit

## Roadmap

Near-term work is focused on:

- expanding the Angular UI to cover workouts, rewards, and leaderboard flows
- continuing to connect the existing backend APIs to feature-complete frontend screens
- hardening deployment automation and operational documentation

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, workflow, and pull request guidance.

## License

This project is licensed under the [MIT License](LICENSE).
