# Bug 016: 404 / "Page not found" route is bare and does not match the design system

## Status
Complete

## Severity
Low

## Area
Error / not-found state

## References
- Implementation: any unknown route (e.g. http://localhost:4321/does-not-exist)
- Design mock: closest analogue is `file:///C:/projects/forge/docs/mocks/error-state.html`
- Screenshots: `docs/screenshots/{desktop,mobile}/not-found.png`

## Description
Hitting an unknown route renders a centered "Page not found / The page you tried to open does not exist or has moved. / Go to dashboard" — with **no app chrome** (no top bar, no nav, no avatar) and a blue button instead of the teal primary. The mocks do not include a dedicated 404 mock, but the design system's expectation is clearly that error / empty surfaces share the app shell, hero illustration treatment, and primary-color token used in `error-state.html` and `empty-state.html`.

## Expected Behavior
- The route should render inside the standard app shell (top bar with brand, nav rail / bottom nav).
- A small illustration (similar to the empty-state circular gradient with a `compass_calibration` or `travel_explore` icon) above the title.
- A title in the same display weight as other page titles, a sub paragraph, and a teal primary "Go to dashboard" CTA + an outlined "Go back" CTA.
- Should fill the viewport on desktop (similar to Bug 007 for sign-in).

## Actual Behavior
- Renders as a bare body with no app chrome.
- Single blue (not teal) "Go to dashboard" button.
- No illustration, no secondary action.
- Does not fill the viewport vertically — large empty area below.

## Proposed Fix
- Move the not-found route inside the authenticated layout (app shell).
- Build a small `NotFoundPageComponent` that mirrors the empty-state visual language: round gradient + icon, title, sub, two CTAs.
- Use the primary teal token (Bug 009) for the filled CTA.
