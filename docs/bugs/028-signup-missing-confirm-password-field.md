# Bug 028: Sign-up form is missing the Confirm password field

## Status
Open

## Severity
Medium

## Area
Auth / sign-up

## References
- Implementation: http://localhost:4321/sign-up
- Design mock: `file:///C:/projects/forge/docs/mocks/sign-up.html` (line 115–116)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/sign-up.png`

## Description
The mock sign-up form has five inputs: First name, Last name, Email, Password, **Confirm password**. The implementation only renders the first four. There is no second password field, so a user cannot catch a typo before submitting account creation, and the form does not match the documented credential-confirmation pattern from the mock.

## Expected Behavior
- Render a `Confirm password*` field directly below the `Password*` field, of the same type/visual treatment.
- Validate that `password === confirmPassword` before enabling Create account.
- Surface a field-level error ("Passwords do not match") when the values diverge after blur.

## Actual Behavior
- Only one password field is rendered.
- A user can submit account creation after a single password entry; there is no client-side confirmation step.

## Proposed Fix
- Add a `confirmPassword` form control (required, matches `password`) to the sign-up reactive form.
- Render a second `mat-form-field` (or equivalent) under Password using the same outline appearance.
- Wire a cross-field validator that flags `mismatch` and disables Create account until both inputs are non-empty and equal.
