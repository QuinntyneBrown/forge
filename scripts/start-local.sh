#!/usr/bin/env bash
# Starts the ForgeFit backend and frontend together for local dev.
#
# Usage: ./scripts/start-local.sh [frontend-port]
#
# 1. Builds + runs the backend in the background, logging to
#    /tmp/forge-api.log. The API listens on https://localhost:5001.
# 2. Polls /health until ready (2 minute ceiling).
# 3. Runs `npm install` once in frontend/ if needed, then launches
#    `npx ng serve forge` in the foreground. Ctrl+C terminates the
#    dev server AND the backend.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
FRONTEND_PORT="${1:-4321}"

cleanup() {
  if [[ -n "${API_PID:-}" ]] && kill -0 "$API_PID" 2>/dev/null; then
    echo "==> Stopping backend (pid $API_PID)"
    kill "$API_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

echo "==> Starting backend (https://localhost:5001)"
cd "$REPO_ROOT/backend"
dotnet run --project src/Forge.Api > /tmp/forge-api.log 2>&1 &
API_PID=$!

echo "==> Waiting for backend /health (pid $API_PID, log /tmp/forge-api.log)"
deadline=$(( $(date +%s) + 120 ))
until curl -sf http://localhost:5000/health > /dev/null 2>&1; do
  if (( $(date +%s) > deadline )); then
    echo "Backend did not respond within 2 minutes. See /tmp/forge-api.log."
    exit 1
  fi
  sleep 2
done
echo "==> Backend ready"

cd "$REPO_ROOT/frontend"
if [[ ! -d node_modules ]]; then
  echo "==> Installing frontend dependencies (first run)"
  npm install
fi

echo "==> Starting frontend on http://localhost:$FRONTEND_PORT"
exec npx ng serve forge --port "$FRONTEND_PORT"
