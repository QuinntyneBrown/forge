# Account and Security

Forge Fit uses local email and password accounts, JWT access tokens, and refresh tokens. External identity providers and PKCE flows are not part of the current app.

## Register a New Account

1. Open `/sign-up`.
2. Enter your first name.
3. Enter your last name.
4. Enter your email address.
5. Enter a password.
6. Select Create account.

Password rules:

- At least 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one symbol

After registration, Forge signs you in and opens `/dashboard`.

Behind the scenes:

- Email is normalized to lowercase.
- Passwords are stored as bcrypt hashes with work factor 12.
- The new account receives the `User` role.
- Forge returns an access token and refresh token.

## Sign In

1. Open `/sign-in`.
2. Enter your email address.
3. Enter your password.
4. Optionally check Remember me.
5. Select Sign in.

If credentials are valid, Forge opens `/dashboard` and displays the signed-in email and role in the dashboard greeting.

If credentials are invalid, the form shows a generic sign-in error. The response does not reveal whether the email or password was wrong.

## Use Remember Me

Remember me controls whether the refresh token is saved across browser restarts.

When Remember me is checked:

1. Forge stores the refresh token in browser local storage.
2. On the next app load, Forge exchanges it for a fresh access token and refresh token.
3. A valid refresh lets you open protected routes without signing in again.

When Remember me is unchecked:

1. Forge keeps the active session in memory only.
2. Closing or refreshing into a new browser context loses the session.
3. Protected routes redirect back to sign-in.

## Sign Out

1. Open `/dashboard`.
2. Select Sign out.

Forge sends the active refresh token to the backend for revocation, clears the client-side session, and returns you to `/sign-in`.

If the refresh token is missing or the backend call fails, the app still clears the local session and routes to sign-in.

## Reset a Forgotten Password

The password reset has two screens: request and confirm.

### Request a Reset Link

1. Open `/password-reset`.
2. Enter the email address for the account.
3. Select Send reset link.
4. Read the confirmation message.

Forge returns the same confirmation whether the account exists or not. This prevents account enumeration.

In the current MVP, the reset email sender logs the token instead of sending a real email. For local development, copy the token from the API log and open:

```text
/password-reset?token=<token>
```

Reset tokens expire after 30 minutes and can be used only once.

### Confirm the Reset

1. Open `/password-reset?token=<token>`.
2. Enter a new password that passes the password policy.
3. Select Save new password.
4. Sign in with the new password.

After a successful reset, Forge revokes existing refresh tokens for the account.

## Delete Your Account

1. Sign in.
2. Open `/profile`.
3. Scroll to the Danger zone section.
4. Select Delete account.
5. Read the confirmation prompt.
6. Select Yes, delete my account.

Forge signs you out immediately.

Behind the scenes:

- The account is soft-deleted.
- The email is replaced with a deleted-account sentinel address.
- First and last name are replaced with `Deleted User`.
- Password hash is cleared.
- Refresh tokens are revoked.
- A security audit event is written.

The old email and password should no longer sign in.

## Failed Sign-In Lockout

Forge tracks failed sign-in attempts by email over a 15-minute window.

1. After 5 failed attempts in that window, the next attempt is rejected with a temporary lockout.
2. The backend returns HTTP 429 with a `Retry-After` header.
3. A successful sign-in before the lockout threshold clears the failure counter.

## Token Behavior

Access tokens:

- Are JWT bearer tokens.
- Include issuer, audience, subject, email, role, not-before, expiration, and JWT id claims.
- Expire after 60 minutes by default.
- Are sent on API calls as `Authorization: Bearer <token>`.

Refresh tokens:

- Are opaque random tokens.
- Are stored hashed in the database.
- Last 14 days by default.
- Rotate on use.
- Revoke the token family if a consumed token is reused.

## Audit and Logging

Forge records audit events for account and security-sensitive actions, including:

- Registration success
- Sign-in success
- Sign-in failure
- Sign-in lockout
- Sign-out
- Password reset success or failure
- Refresh token success or failure
- Account deletion

Application logs are structured JSON. The logging layer redacts sensitive fields such as passwords and tokens before output.

## Security Headers

The API adds baseline security headers:

- `Content-Security-Policy`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: no-referrer`
- `Strict-Transport-Security` outside Development

In non-development environments, bearer token validation requires HTTPS metadata.
