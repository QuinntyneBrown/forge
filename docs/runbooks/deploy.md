# Deploying ForgeFit to Azure

## TL;DR

1. `az login && az account set --subscription <id>`
2. Set the secrets:
   - `FORGE_SQL_PASSWORD` (≥12 chars, mixed case + digit + symbol)
   - `FORGE_JWT_SIGNING_KEY` (≥32 random bytes, base64-encoded)
3. `./scripts/provision-azure.sh` — creates the resource group, App Service, SQL Server, SQL DB, and Static Web App.
4. Add the GitHub repo secrets the deploy workflow needs (see [Required GitHub secrets](#required-github-secrets)).
5. Push to `main` (or run `deploy.yml` via "Run workflow") to deploy.

## Resources provisioned

| Component       | Resource                                  | SKU        | Approx cost / month |
|-----------------|-------------------------------------------|------------|---------------------|
| Resource group  | `rg-forgefit-prod` in East US 2           | n/a        | $0                  |
| App Service plan| `asp-forgefit-prod` (Linux)               | **B1 Basic** | ~$13               |
| Web App         | `app-forgefit-api-prod` (.NET 9)          | included   | n/a                 |
| Azure SQL Server| `sql-forgefit-prod`                       | n/a        | $0                  |
| Azure SQL DB    | `forgefit`                                | **S0 Standard** (10 DTU) | ~$15 |
| Static Web App  | `swa-forgefit-prod`                       | **Free**   | $0                  |
| **Total**       |                                           |            | **~$28 / month**   |

### Why these SKUs are the cheapest viable plan

- **App Service B1 over F1 (Free):** the `NotificationDispatcherHostedService` (BT-032) needs always-on. F1 cycles the worker, so the dispatcher would only fire when an HTTP request happens to wake the app. B1 is the cheapest tier with always-on.
- **SQL S0 over Basic:** the dashboard summary (BT-029) issues six aggregation reads per hit. Basic (5 DTU) has measurable contention under the test seed; S0 (10 DTU) keeps the local-200ms budget realistic.
- **Static Web App Free:** the Angular bundle is well under the 100 MB Free-tier asset cap and the ten-route count is well under the 100-route cap.

Substitute Container Apps + Azure Database for PostgreSQL Flexible Server (B-series, paused outside business hours) for ~half the cost if your environment can tolerate the cold-start latency on the API.

## Provisioning script

`./scripts/provision-azure.sh` is the executable contract. It is idempotent: re-running it against an already-provisioned resource group is safe (Azure CLI updates settings to match).

### Required environment variables

| Variable                  | Required | Default                      |
|---------------------------|----------|------------------------------|
| `FORGE_SQL_PASSWORD`      | ✅       | —                            |
| `FORGE_JWT_SIGNING_KEY`   | ✅       | —                            |
| `FORGE_ENV`               |          | `prod`                       |
| `FORGE_LOCATION`          |          | `eastus2`                    |
| `FORGE_RG`                |          | `rg-forgefit-${FORGE_ENV}`   |
| `FORGE_APP_PLAN`          |          | `asp-forgefit-${FORGE_ENV}`  |
| `FORGE_API_APP`           |          | `app-forgefit-api-${FORGE_ENV}` |
| `FORGE_SQL_SERVER`        |          | `sql-forgefit-${FORGE_ENV}`  |
| `FORGE_SQL_DB`            |          | `forgefit`                   |
| `FORGE_SQL_ADMIN`         |          | `forgeadmin`                 |
| `FORGE_SWA`               |          | `swa-forgefit-${FORGE_ENV}`  |
| `FORGE_GITHUB_REPO`       |          | `QuinntyneBrown/forge`       |
| `FORGE_GITHUB_BRANCH`     |          | `main`                       |

### What the script writes to the web app

- `ConnectionStrings__DefaultConnection` — built from the SQL server, DB, admin user, and password.
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` — sourced from `FORGE_JWT_SIGNING_KEY`.
- `Cors__AllowedOrigins__0` — set to `https://${FORGE_SWA}.azurestaticapps.net`.

If you need extra origins (e.g. a preview branch), add them in the App Service blade or extend the script with an env-driven list.

## CI/CD workflow

`.github/workflows/deploy.yml` runs on every push to `main` and via manual `workflow_dispatch`. Two parallel jobs:

- **api** — `dotnet publish src/Forge.Api`, then `azure/webapps-deploy@v3` against `app-forgefit-api-prod`. Uses the `AZURE_CREDENTIALS` secret (service principal JSON).
- **web** — `npm ci && npm run build` in `frontend/`, then `Azure/static-web-apps-deploy@v1` with `skip_app_build: true` (we already built). Uses the `AZURE_STATIC_WEB_APPS_API_TOKEN` secret (created when the SWA is provisioned).

The first push provisions nothing — the script must run first. The workflow only deploys.

### Required GitHub secrets

| Secret                              | Value                                               |
|-------------------------------------|-----------------------------------------------------|
| `AZURE_CREDENTIALS`                 | `az ad sp create-for-rbac --sdk-auth` JSON          |
| `AZURE_STATIC_WEB_APPS_API_TOKEN`   | `az staticwebapp secrets list --query "properties.apiKey" -o tsv` |

## Migrations

`Forge.Api/Program.cs` runs `await db.Database.MigrateAsync()` at startup, so deploys auto-apply any new migrations. There is no separate migration step in the workflow.

## Rollback

Three layers, ranked from cheapest to most disruptive:

1. **Re-deploy the previous `main` commit.** GitHub Actions retains workflow history; `workflow_dispatch` against an older `ref` reverts to the prior bundle. App Service keeps two deployment slots warm via the deploy step's atomic swap, so users never see a 502 during the swap.
2. **Roll back a migration manually.** Connect to SQL with `sqlcmd` against `sql-forgefit-prod`, run `__EFMigrationsHistory` queries, and apply the inverse SQL by hand. EF Core does not provide automatic down-migrations in production. Migration script template:
   ```sh
   dotnet ef migrations script <PreviousMigration> <BadMigration> \
     --project src/Forge.Infrastructure --startup-project src/Forge.Api --idempotent --output rollback.sql
   ```
   then run `sqlcmd -S sql-forgefit-prod.database.windows.net -d forgefit -U forgeadmin -i rollback.sql`.
3. **Restore from backup.** Azure SQL has automatic point-in-time restore enabled by default (7 days for S0 with `--backup-storage-redundancy Local`). Restore to a new database name, swap the `ConnectionStrings__DefaultConnection` value on the web app, restart.

## Repeating the deploy from scratch

To rebuild the entire deployment from a fresh subscription:

```sh
az login
az account set --subscription <subscription-id>
export FORGE_SQL_PASSWORD='<strong-password>'
export FORGE_JWT_SIGNING_KEY='<base64-32-bytes>'
./scripts/provision-azure.sh
# Then add the two GitHub secrets, push to main, and the workflow deploys.
```

That single command sequence is the source of truth — the runbook exists to explain *why* each step is there. Update both this doc and the script together if any value moves.
