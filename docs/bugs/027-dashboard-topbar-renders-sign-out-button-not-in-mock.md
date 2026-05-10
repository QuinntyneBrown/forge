# Bug 027: Dashboard renders a "Sign out" pill in the page header that doesn't exist in the mock

## Status
Open

## Severity
Low

## Area
Dashboard / app shell

## References
- Implementation: http://localhost:4321/dashboard
- Source: `frontend/projects/forge/src/app/pages/dashboard/dashboard.page.html:12-13`, `dashboard.page.scss:88` (`.dashboard__sign-out`)
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html` (no Sign out CTA anywhere; mock topbar has only menu / brand / notifications / avatar)
- Screenshots: `docs/screenshots/{desktop,tablet,mobile}/dashboard.png`

## Description
The implemented `/dashboard` route puts a `Sign out` outlined-pill button to the right of the greeting block on every viewport. The mock has no such button on the dashboard — the only sign-out affordance lives inside the Profile page's Save card (per Bug 011's resolution). The dashboard sign-out is dead weight that competes for attention with the hero CTA and Sign-out is already present on Profile.

## Expected Behavior
- Dashboard greeting row contains only `Good morning, / Quinntyne / Tuesday, May 12 …`.
- No top-level Sign out button on the dashboard.
- The single sign-out lives inside `/profile`'s Save card (already present).

## Actual Behavior
- A `Sign out` outlined pill appears on the right side of the greeting row at every viewport (visible in `desktop/dashboard.png`, `tablet/dashboard.png`, `mobile/dashboard.png`).

## Proposed Fix
- Remove the `<button class="dashboard__sign-out">` block from `dashboard.page.html`.
- Remove the `signOut()` method from `DashboardPage` (it duplicates the one on `ProfilePage`).
- Drop the `.dashboard__sign-out` rule from `dashboard.page.scss`.
- Update or delete the dashboard sign-out e2e spec (`data-testid="sign-out"` on dashboard) — move it to the profile spec if not already covered.
