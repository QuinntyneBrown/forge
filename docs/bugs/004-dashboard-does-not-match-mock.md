# Bug 004: Dashboard page does not align with design mock

## Severity
High

## Area
Dashboard

## References
- Implementation: http://localhost:4321/dashboard
- Design mock: `file:///C:/projects/forge/docs/mocks/dashboard.html`

## Description
The implemented `/dashboard` page diverges significantly from the authoritative design mock at `docs/mocks/dashboard.html`. Multiple expected elements are either missing or incorrectly styled.

## Missing / Incorrect Elements

### Header
- Avatar is missing.
- Hamburger menu is missing.
- Notification bell is missing.
- Header is not sticky — it should remain pinned to the top on scroll.

### Dashboard body
- "+ Log workout" floating action button (FAB) is missing.
- Greeting block is missing — should display "Good morning, {Username}" along with the current date/time.
- Rich information cards (with the colors and content shown in the mock) are missing.

## Expected Behavior
The implemented dashboard should match `docs/mocks/dashboard.html` pixel-for-pixel, including layout, typography, color, spacing, and all interactive elements listed above.

## Proposed Fix
- Implement the header with avatar, hamburger menu, notification bell, and sticky positioning.
- Add the greeting + date/time block.
- Add the "+ Log workout" FAB.
- Add the rich cards with the correct colors, content, and layout per the mock.
- Run a UI audit (`/ui-audit`) against the dashboard once changes land.
