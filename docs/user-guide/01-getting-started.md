# Getting Started

This document explains how to open Forge Fit locally, create or use an account, and move around the app.

## Prerequisites

To run Forge Fit from this repository, install:

- .NET SDK 9.0.x
- Node.js 20+
- npm
- SQL Server, SQL Server Express, or LocalDB reachable by the backend connection string

The default backend connection string is in `backend/src/Forge.Api/appsettings.json`.

## Start Forge Fit With the Helper Script

On Windows PowerShell:

```powershell
.\scripts\start-local.ps1
```

On bash-compatible shells:

```bash
./scripts/start-local.sh
```

The helper scripts start:

- Backend API: `https://localhost:5001`
- Frontend app: `http://localhost:4321`

The scripts wait for the backend health endpoint before starting the frontend. The Windows script opens the backend in another PowerShell window. The bash script starts the backend in the background and stops it when the foreground frontend server exits.

## Start the Backend Manually

1. Open a terminal at the repository root.
2. Change to the backend folder:

   ```bash
   cd backend
   ```

3. Restore packages:

   ```bash
   dotnet restore
   ```

4. Start the API:

   ```bash
   dotnet run --project src/Forge.Api
   ```

5. Confirm the health endpoint responds:

   ```bash
   curl http://localhost:5000/health
   ```

   Expected response:

   ```json
   { "status": "Healthy" }
   ```

The API applies Entity Framework migrations at startup. In Development, it also seeds the development user if it does not already exist.

## Start the Frontend Manually

1. Open a second terminal at the repository root.
2. Change to the frontend folder:

   ```bash
   cd frontend
   ```

3. Install dependencies:

   ```bash
   npm ci
   ```

4. Start the Angular app:

   ```bash
   npm start
   ```

5. Open `http://localhost:4200`.

If you use the helper script instead, open `http://localhost:4321`.

## Sign In With the Development Account

When the backend runs in Development, Forge seeds this account:

1. Go to `/sign-in`.
2. Enter `dev@forge.local`.
3. Enter `DevPassword123!`.
4. Choose whether to check Remember me.
5. Select Sign in.

Forge opens the dashboard after a successful sign-in.

## Create a New Account

1. Go to `/sign-up`.
2. Enter first name, last name, email, and password.
3. Use a password with at least 12 characters, one uppercase letter, one lowercase letter, one digit, and one symbol.
4. Select Create account.

Forge creates the account, signs you in, and opens the dashboard.

## Move Around the App

After signing in, use these destinations:

- Home opens the dashboard at `/dashboard`.
- Workouts opens the workout log at `/workouts`.
- Rewards opens the rewards catalog at `/rewards`.
- Profile opens account settings at `/profile`.

At mobile widths, these destinations appear in the bottom navigation bar. At desktop widths, they appear in the left navigation rail.

## Protected Pages

The dashboard, workouts, rewards, and profile routes require a signed-in session. If you open a protected route while signed out, Forge sends you to `/sign-in` with a `returnUrl` query string.

## Recommended First Session

1. Sign in.
2. Open Workouts.
3. Select New session.
4. Choose Treadmill.
5. Enter a duration such as `22`.
6. Enter distance, average heart rate, active calories, and optional notes.
7. Select Save session.
8. Return to Dashboard and confirm the calorie ring, minutes, points balance, streak, and tier cards update.
