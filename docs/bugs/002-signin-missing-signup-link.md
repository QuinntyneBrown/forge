# Bug 002: No link on sign-in page to sign up

## Status
Complete

## Severity
Medium

## Area
Auth / Sign-in page

## Description
The sign-in page does not include a link to the sign-up page. New users have no obvious path to create an account from the sign-in screen.

## Expected Behavior
The sign-in page should include a clearly visible link such as "Don't have an account? Sign up" that navigates to the sign-up page. This should match the design mock.

## Actual Behavior
No sign-up link is rendered on the sign-in page.

## Required Work
- Update mocks (`docs/mocks/`) to include the sign-up link on the sign-in page.
- Regenerate the sign-in screenshot to reflect the updated mock.
- Implement the link in the UI (sign-in page component) pointing to the sign-up route.
- Update e2e tests to cover the navigation.
