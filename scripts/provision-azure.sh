#!/usr/bin/env bash
# Provisions the cheapest viable Azure resources for ForgeFit.
#
# Usage:
#   ./scripts/provision-azure.sh                            # uses defaults
#   FORGE_ENV=staging ./scripts/provision-azure.sh
#
# Requires: az CLI, an authenticated session (`az login`), and an
# active subscription set via `az account set --subscription <id>`.
#
# Cheapest-plan SKU choices (documented in docs/runbooks/deploy.md):
#   - Resource group:    East US 2 (low latency from typical dev region).
#   - App Service plan:  Linux B1 (Basic, ~$13/mo). F1 (Free) drops the
#                        custom-domain + always-on capability we need
#                        for the notification dispatcher hosted service.
#   - SQL Database:      Azure SQL S0 (Standard, 10 DTU ~$15/mo). The
#                        Basic tier is cheaper but caps at 5 DTU which
#                        is too tight for the dashboard query.
#   - Static Web App:    Free tier — covers the Angular app's needs.

set -euo pipefail

ENV_NAME="${FORGE_ENV:-prod}"
LOCATION="${FORGE_LOCATION:-eastus2}"
RG="${FORGE_RG:-rg-forgefit-${ENV_NAME}}"
APP_PLAN="${FORGE_APP_PLAN:-asp-forgefit-${ENV_NAME}}"
API_APP="${FORGE_API_APP:-app-forgefit-api-${ENV_NAME}}"
SQL_SERVER="${FORGE_SQL_SERVER:-sql-forgefit-${ENV_NAME}}"
SQL_DB="${FORGE_SQL_DB:-forgefit}"
SQL_ADMIN="${FORGE_SQL_ADMIN:-forgeadmin}"
SQL_PASSWORD="${FORGE_SQL_PASSWORD:?FORGE_SQL_PASSWORD must be set}"
JWT_SIGNING_KEY="${FORGE_JWT_SIGNING_KEY:?FORGE_JWT_SIGNING_KEY must be set}"
SWA_NAME="${FORGE_SWA:-swa-forgefit-${ENV_NAME}}"
GITHUB_REPO="${FORGE_GITHUB_REPO:-QuinntyneBrown/forge}"
GITHUB_BRANCH="${FORGE_GITHUB_BRANCH:-main}"

echo "==> Resource group $RG in $LOCATION"
az group create --name "$RG" --location "$LOCATION" >/dev/null

echo "==> Linux B1 App Service plan $APP_PLAN"
az appservice plan create \
  --resource-group "$RG" \
  --name "$APP_PLAN" \
  --is-linux \
  --sku B1 >/dev/null

echo "==> .NET 9 web app $API_APP"
az webapp create \
  --resource-group "$RG" \
  --plan "$APP_PLAN" \
  --name "$API_APP" \
  --runtime "DOTNETCORE:9.0" >/dev/null

echo "==> Azure SQL server $SQL_SERVER"
az sql server create \
  --resource-group "$RG" \
  --name "$SQL_SERVER" \
  --location "$LOCATION" \
  --admin-user "$SQL_ADMIN" \
  --admin-password "$SQL_PASSWORD" >/dev/null

echo "==> SQL firewall: allow Azure services"
az sql server firewall-rule create \
  --resource-group "$RG" \
  --server "$SQL_SERVER" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 >/dev/null

echo "==> Azure SQL S0 database $SQL_DB"
az sql db create \
  --resource-group "$RG" \
  --server "$SQL_SERVER" \
  --name "$SQL_DB" \
  --service-objective S0 \
  --backup-storage-redundancy Local >/dev/null

CONNECTION_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=${SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"

echo "==> Web app application settings"
az webapp config appsettings set \
  --resource-group "$RG" \
  --name "$API_APP" \
  --settings \
    "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    "Jwt__Issuer=forge.app" \
    "Jwt__Audience=forge.app" \
    "Jwt__SigningKey=$JWT_SIGNING_KEY" \
    "Cors__AllowedOrigins__0=https://${SWA_NAME}.azurestaticapps.net" \
  >/dev/null

echo "==> Static Web App (Free) $SWA_NAME"
az staticwebapp create \
  --resource-group "$RG" \
  --name "$SWA_NAME" \
  --location "$LOCATION" \
  --sku Free \
  --source "https://github.com/${GITHUB_REPO}" \
  --branch "$GITHUB_BRANCH" \
  --app-location "frontend" \
  --output-location "dist/forge/browser" \
  --login-with-github >/dev/null

API_HOST="$(az webapp show --resource-group "$RG" --name "$API_APP" --query defaultHostName -o tsv)"
SWA_HOST="$(az staticwebapp show --resource-group "$RG" --name "$SWA_NAME" --query defaultHostname -o tsv)"

echo
echo "Provision complete."
echo "  API:           https://${API_HOST}"
echo "  Static Web App: https://${SWA_HOST}"
echo
echo "Next: push to '${GITHUB_BRANCH}' to trigger ./.github/workflows/deploy.yml"
