# Bug 003: "Show password" toggle on sign-in page does not work

## Severity
Medium

## Area
Auth / Sign-in page

## Description
The "show password" eye-icon toggle on the sign-in page does not reveal the password. Clicking it has no visible effect on the password input.

## Steps to Reproduce
1. Navigate to the sign-in page.
2. Type a value into the password field.
3. Click the show-password (eye) icon.

## Expected Behavior
Clicking the icon should toggle the password input's `type` between `password` and `text`, so the user can verify what they typed. The icon should also reflect the current state (e.g. eye vs eye-with-slash).

## Actual Behavior
Clicking the icon does nothing — the password remains masked.

## Proposed Fix
- Bind the icon's click handler to a local `showPassword` state.
- Switch the input's `type` based on that state.
- Swap the icon to reflect the current visibility.
- Add a test asserting the toggle behavior.
