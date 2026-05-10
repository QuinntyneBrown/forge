# Bug 001: "Remember me" on sign-in page does not work

## Status
Complete

## Severity
Medium

## Area
Auth / Sign-in page

## Description
The "Remember me" checkbox on the sign-in page has no effect. Toggling it does not change session persistence behavior.

## Steps to Reproduce
1. Navigate to the sign-in page.
2. Enter valid credentials.
3. Tick the "Remember me" checkbox.
4. Sign in.
5. Close the browser/tab and reopen the app.

## Expected Behavior
With "Remember me" checked, the user's session should persist across browser restarts (long-lived session/refresh token). With it unchecked, the session should expire when the browser session ends.

## Actual Behavior
The checkbox state is not honored. Session persistence behavior is identical regardless of whether the box is checked.

## Proposed Fix
- Wire the checkbox value into the sign-in request payload / auth client.
- Adjust token storage strategy (e.g. `localStorage` vs `sessionStorage`, or refresh-token TTL) based on the flag.
- Add a test asserting both modes.
