# Forge Fit

Forge Fit is an open source fitness gamification project focused on building better morning routines, discouraging late-night eating, and turning consistent training into a rewarding feedback loop.

The repository is currently in an early stage: product direction, workflow, and design artifacts are in place, with backend and frontend solution structure beginning to take shape.

## Why Forge Fit?

Forge Fit is being designed for at-home fitness routines built around equipment like:

- treadmill
- indoor bike
- bench press
- elliptical

The product vision combines habit support, workout tracking, and game-like rewards to help users stay consistent and see progress over time.

## Current Project Status

**Status:** early-stage / pre-release

What is available today:

- a product brief in `docs/idea.md`
- static HTML mocks for core app flows in `docs/mocks/`
- generated screenshots for multiple viewport sizes in `docs/mocks/screenshots/`
- a Playwright-based rendering script for regenerating mock screenshots
- a root tooling manifest for design workflows
- an initial backend solution file at `backend/Forge.sln`

## Planned Capabilities

Forge Fit is intended to support:

- authentication and profile management
- workout logging and workout history
- calorie, minutes, and streak tracking
- rewards and achievement mechanics
- Apple Watch-aware fitness workflows
- structured product, QA, and deployment documentation

## Repository Layout

| Path | Purpose |
| --- | --- |
| `backend/` | .NET solution root |
| `frontend/` | frontend workspace root |
| `docs/idea.md` | product brief |
| `docs/mocks/` | static product mocks |
| `docs/mocks/screenshots/` | rendered mock screenshots |
| `docs/specs/` | requirements documents |
| `docs/plans/` | implementation plans |
| `docs/runbooks/` | local run and deployment guides |
| `docs/qa/` | QA artifacts |
| `docs/evaluations/` | evaluation notes |
| `package.json` | root tooling scripts |

## Getting Started

### Prerequisites

- Node.js 18+
- npm
- .NET SDK

### Install root tooling

```bash
npm install
```

### Render mock screenshots

```bash
npm run render-mocks
```

If Playwright's Chromium browser is not installed yet:

```bash
npx playwright install chromium
```

### Review the current UI mocks

Open `docs/mocks/index.html` in a browser.

## Development Notes

The root `package.json` is for repository tooling. Application implementation is expected to live under `backend/` and `frontend/`.

## Contributing

Contributions are welcome, especially around:

- product requirements
- UX and design review
- implementation planning
- backend and frontend scaffolding

Please keep pull requests focused and include clear context for any product or UI changes.

## License

This repository does not currently include a license file. Until one is added, treat the contents as all rights reserved.
