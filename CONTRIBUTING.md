# Contributing

Thanks for contributing to Forge Fit.

## Development setup

### Backend

```bash
cd backend
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Before running the API locally, set a real JWT signing key in `backend/src/Forge.Api/appsettings.json`.

### Frontend

```bash
cd frontend
npm ci
npm run build
```

To run the app locally:

```bash
npm start
```

To run the end-to-end suite:

```bash
npm run e2e
```

## Project structure

- `backend/` contains the .NET solution and backend acceptance tests
- `frontend/` contains the Angular app, reusable libraries, and Playwright tests
- `docs/` contains product, design, planning, QA, and workflow artifacts

## Contribution guidelines

1. Keep changes focused and scoped to a clear problem.
2. Follow the existing architecture instead of introducing parallel patterns.
3. Update documentation when behavior or developer workflow changes.
4. Include tests when changing backend or frontend behavior.
5. Keep pull requests easy to review with a clear summary of what changed and why.

## Pull requests

Please include:

- a concise summary
- screenshots for visible UI changes when relevant
- notes about any setup, migration, or configuration impact

## Code of conduct

Be respectful, constructive, and collaborative in code reviews and discussions.
