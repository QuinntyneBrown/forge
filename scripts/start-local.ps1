<#
.SYNOPSIS
    Starts the ForgeFit backend and frontend together for local dev.

.DESCRIPTION
    1. Restores + builds the backend, then launches the API in a new
       PowerShell window listening on https://localhost:5001.
    2. Polls /health until the API responds (2-minute ceiling).
    3. Runs `npm install` once in frontend/, then launches `npx ng serve forge`
       in this terminal so Ctrl+C stops the dev server cleanly.

    The backend window stays open after this script exits — close it
    (or kill the dotnet process) when you're done.

.PARAMETER FrontendPort
    The port for `ng serve`. Defaults to 4321.

.EXAMPLE
    PS> ./scripts/start-local.ps1
#>
param(
    [int]$FrontendPort = 4321
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

# Backend: launch in a new window so Ctrl+C in this shell only kills the
# foreground frontend dev server.
Write-Host "==> Starting backend (https://localhost:5001) in a new window"
Start-Process pwsh -ArgumentList @(
    '-NoExit',
    '-Command',
    "Set-Location '$repoRoot/backend'; dotnet run --project src/Forge.Api"
)

Write-Host "==> Waiting for backend /health …"
$deadline = (Get-Date).AddMinutes(2)
while ((Get-Date) -lt $deadline) {
    try {
        # -SkipCertificateCheck handles the dev-only self-signed cert.
        $resp = Invoke-WebRequest -Uri 'http://localhost:5000/health' -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) { break }
    } catch {
        Start-Sleep -Seconds 2
    }
}

# Frontend
Set-Location "$repoRoot/frontend"
if (-not (Test-Path 'node_modules')) {
    Write-Host "==> Installing frontend dependencies (first run)"
    npm install
}
Write-Host "==> Starting frontend on http://localhost:$FrontendPort"
npx ng serve forge --port $FrontendPort
