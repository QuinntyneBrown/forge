# Bug 020: Auth screens on mobile/tablet are missing the curved hero, overlapping card, and pill primary button

## Status
Complete

## Severity
Medium

## Area
Auth (sign-in, sign-up, password-reset)

## References
- Implementation: http://localhost:4321/{sign-in,sign-up,password-reset}
- Design mocks: `file:///C:/projects/forge/docs/mocks/sign-in.html`, `sign-up.html`, `password-reset.html`
- Screenshots: `docs/screenshots/{mobile,tablet}/{sign-in,sign-up,password-reset}.png`

## Description
On mobile, the auth mocks share a distinctive layout: a teal radial-gradient hero with rounded bottom corners (`border-bottom-{left,right}-radius: 28px`), and the form card pulls up over the hero (`margin-top: -20px`) so its top edge overlaps the curved hero. The implementation renders a flat top hero with **square** corners and the form card sitting fully below it with no overlap. The primary button is a flat blue rectangle instead of the rounded pill (border-radius: 999px) in the mock's teal.

Additional auth-specific gaps:
- Sign-up mock includes a 3-row "perks" list inside the hero (`-20 lb / month target`, `1,500 active calories per day`, `Earn points for every session`) — implementation has no perks.
- Sign-up mock shows a strength meter (4-bar segmented) below the password field with a "Strong — 12 characters, mixed case, numbers" caption — implementation shows a static helper sentence only.
- Sign-up mock includes a checked Terms-of-Service / Privacy checkbox above the submit — implementation has no ToS checkbox.
- Password-reset mock has a "Step 1 / Step 2" sectioned UI with a confirmation card (sent timestamp badge, info list, Resend button); implementation is single-step only.
- All auth pages tablet (768px) layout: implementation cuts the teal hero off vertically (large empty teal area below the title) instead of letting it stretch to full viewport height. See `tablet/sign-in.png`.

## Expected Behavior
Per the auth mocks:
- Mobile: hero has `border-bottom-{left,right}-radius: 28px`, card has `margin-top: -20px` to overlap.
- All viewports: primary submit button is pill-shaped (radius 999px) and uses the teal token.
- Tablet (>=768px): the layout becomes a 2-column grid (`grid-template-columns: 1fr 1fr; min-height: 100vh`) — hero stretches the full viewport height on the left.
- Sign-up: hero perks list, password strength meter, ToS checkbox.
- Password-reset: confirmation/sent state visible (either as a 2nd step card stacked below in the mock-style preview, or as a state transition after submit).

## Actual Behavior
- Mobile hero has square corners; card sits below with a visible gap.
- Submit buttons are square-ish blue rectangles.
- Tablet hero shows a large empty teal column beneath the title — does not stretch / does not show perks (sign-up).
- No password strength meter, no ToS checkbox on sign-up.
- No confirmation/sent state on password-reset.

## Proposed Fix
- Add `border-bottom-radius: 28px` and the `-20px` margin-top overlap on the mobile auth card; clip-path or background fix to keep the gradient visible behind the card top.
- Switch primary submit button to the pill style + teal token (overlaps with Bug 009).
- Sign-up: render perks inside the hero, add a `PasswordStrengthMeterComponent`, add a ToS checkbox bound to a required form control.
- Password-reset: after submit, show the confirmation card with the email and a Resend button.
- Tablet: ensure the auth grid becomes 2-col with `min-height: 100vh` so the hero fills its half of the viewport.
